using PtzStudio.Core.Models;
using PtzStudio.Core.Motion;
using PtzStudio.Core.Visca;

namespace PtzStudio.Core.Control;

/// <summary>
/// Converte entradas (analógico físico, joystick virtual, botões) em comandos VISCA, com as proteções:
/// parada ao neutro, parada de emergência com prioridade, watchdog de entrada, anti-repetição e troca segura de fonte.
/// </summary>
public sealed class PtzController
{
    public const int WatchdogMs = 600;
    public const int KeepAliveMs = 400;

    private readonly IViscaSender _tx;
    private readonly ILockableSender? _lockable;
    private readonly Func<CameraConfig> _cam;
    private readonly Func<Profile> _profile;
    private readonly MotionProcessor _motion = new();
    private readonly object _lock = new();

    private InputSource _source = InputSource.Fisico;
    private double _ax, _ay, _az;
    private int _digPan, _digTilt, _digZoom, _digFocus;
    private DateTime _lastInput = DateTime.MinValue;
    private int _sentPan, _sentTilt, _sentZoom, _sentFocus;
    private int _stopPendingPt, _stopPendingZoom;
    private DateTime _lastPt = DateTime.MinValue, _lastZoom = DateTime.MinValue;
    private bool _requireNeutral;
    private volatile bool _emergency;   // trava DURA: só TryResume (botão RETOMAR CONTROLE) libera
    private bool _autoFocus;
    private (double x, double y, double z, DateTime until)? _pulse;
    private (int dir, DateTime until)? _focusPulse;
    private int _stopPendingFocus;

    public int SpeedLevelIndex { get; set; } = 1;
    /// <summary>Trava leve (aguarda o neutro): usada em paradas de segurança automáticas (watchdog, controle desconectado...).</summary>
    public bool EmergencyLatched => _requireNeutral;
    /// <summary>Parada de emergência ATIVADA: bloqueia entradas, botões e presets até <see cref="TryResume"/>.</summary>
    public bool EmergencyActive => _emergency;
    public int SentPan => _sentPan;
    public int SentTilt => _sentTilt;
    public int SentZoom => _sentZoom;
    public event Action<string>? Message;

    public PtzController(IViscaSender tx, Func<CameraConfig> cam, Func<Profile> profile)
    {
        _tx = tx; _cam = cam; _profile = profile;
        _lockable = tx as ILockableSender;
    }

    public InputSource ActiveSource
    {
        get => _source;
        set
        {
            lock (_lock)
            {
                if (_source == value) return;
                _source = value;
                StopAllLocked("troca da fonte de entrada");
            }
        }
    }

    /// <summary>Recebe a posição do analógico (físico ou virtual). Entradas da fonte inativa são ignoradas.</summary>
    public void SetAnalog(InputSource src, double x, double y, double z, DateTime now)
    {
        lock (_lock)
        {
            if (_emergency || src != _source) return;
            _ax = x; _ay = y; _az = z; _lastInput = now;
        }
    }

    public void SetDigital(int pan, int tilt, int zoom, int focus)
    {
        lock (_lock) { if (_emergency) return; _digPan = pan; _digTilt = tilt; _digZoom = zoom; _digFocus = focus; }
    }

    /// <summary>Teste de direção: injeta uma entrada por um curto tempo (passa pelas inversões do perfil).</summary>
    public void Pulse(double x, double y, double z, int ms, DateTime now)
    {
        lock (_lock) { if (_emergency) return; _pulse = (x, y, z, now.AddMilliseconds(ms)); _lastInput = now; }
    }

    /// <summary>Teste de foco: aciona o foco por um curto tempo e para (a parada é repetida por segurança).</summary>
    public void PulseFocus(int dir, int ms, DateTime now)
    {
        lock (_lock) { if (_emergency) return; _focusPulse = (Math.Sign(dir), now.AddMilliseconds(ms)); _lastInput = now; }
    }

    public void Tick(double dt, DateTime now)
    {
        lock (_lock)
        {
            var cam = _cam();
            var prof = _profile();
            if (_emergency) { Settle(cam, now); return; }   // emergência: nada além de paradas
            double ax = _ax, ay = _ay, az = _az;

            if (_pulse is { } pl)
            {
                if (now < pl.until) { ax = pl.x; ay = pl.y; az = pl.z; _lastInput = now; }
                else { _pulse = null; ax = ay = az = 0; }
            }
            else
            {
                ax = Math.Clamp(ax + _digPan, -1, 1);
                ay = Math.Clamp(ay + _digTilt, -1, 1);
                az = Math.Clamp(az + _digZoom, -1, 1);
            }

            int focus = _digFocus;
            if (_focusPulse is { } fp)
            {
                if (now < fp.until) { focus = fp.dir; _lastInput = now; }
                else _focusPulse = null;
            }

            bool neutral = Math.Abs(ax) < 0.001 && Math.Abs(ay) < 0.001 && Math.Abs(az) < 0.001 && focus == 0;
            if (_requireNeutral)
            {
                if (neutral) _requireNeutral = false;
                else { Settle(cam, now); return; }
            }

            bool moving = _sentPan != 0 || _sentTilt != 0 || _sentZoom != 0;
            if (moving && _pulse == null && (now - _lastInput).TotalMilliseconds > WatchdogMs)
            {
                EmergencyStopLocked("sem atualização de entrada (watchdog)");
                return;
            }

            var o = _motion.Process(ax, ay, az, dt, prof.Motion);
            var levels = prof.Motion.SpeedLevels;
            double level = levels.Length == 0 ? 1.0 : levels[Math.Clamp(SpeedLevelIndex, 0, levels.Length - 1)];
            int pan = ToInt(o.Pan, level, cam.MaxPanSpeed);
            int tilt = ToInt(o.Tilt, level, cam.MaxTiltSpeed);
            int zoom = ToInt(o.Zoom, level, cam.MaxZoomSpeed);

            ApplyPanTilt(cam, pan, tilt, now);
            ApplyZoom(cam, zoom, now);
            ApplyFocus(cam, focus);
        }
    }

    /// <summary>Único ponto de saída de comandos do controlador. Com a emergência ativa, só paradas e consultas passam.</summary>
    private void Tx(byte[] packet)
    {
        if (_emergency && !ViscaCommands.IsSafeWhileLocked(packet)) return;
        _tx.Send(packet);
    }

    private static int ToInt(double speed, double level, int cameraMax)
    {
        if (Math.Abs(speed) < 0.5) return 0;
        int i = (int)Math.Max(1, Math.Round(Math.Abs(speed) * level));
        return Math.Sign(speed) * Math.Min(i, Math.Max(1, cameraMax));
    }

    private void ApplyPanTilt(CameraConfig cam, int pan, int tilt, DateTime now)
    {
        if (pan == 0 && tilt == 0)
        {
            if (_sentPan != 0 || _sentTilt != 0)
            {
                Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress));
                _sentPan = _sentTilt = 0; _stopPendingPt = 2; _lastPt = now;
            }
            else if (_stopPendingPt > 0)
            {
                Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress));
                _stopPendingPt--;
            }
            return;
        }
        bool changed = pan != _sentPan || tilt != _sentTilt;
        double since = (now - _lastPt).TotalMilliseconds;
        if ((changed && since >= cam.CommandIntervalMs) || (!changed && since >= KeepAliveMs))
        {
            Tx(ViscaCommands.PanTiltDrive(cam.ViscaAddress, pan, tilt, cam.MaxPanSpeed, cam.MaxTiltSpeed));
            _sentPan = pan; _sentTilt = tilt; _lastPt = now; _stopPendingPt = 0;
        }
    }

    private void ApplyZoom(CameraConfig cam, int zoom, DateTime now)
    {
        if (zoom == 0)
        {
            if (_sentZoom != 0)
            {
                Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0));
                _sentZoom = 0; _stopPendingZoom = 2; _lastZoom = now;
            }
            else if (_stopPendingZoom > 0)
            {
                Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0));
                _stopPendingZoom--;
            }
            return;
        }
        bool changed = zoom != _sentZoom;
        double since = (now - _lastZoom).TotalMilliseconds;
        if ((changed && since >= cam.CommandIntervalMs) || (!changed && since >= KeepAliveMs))
        {
            Tx(ViscaCommands.Zoom(cam.ViscaAddress, zoom, cam.MaxZoomSpeed));
            _sentZoom = zoom; _lastZoom = now; _stopPendingZoom = 0;
        }
    }

    private void ApplyFocus(CameraConfig cam, int dir)
    {
        if (dir != _sentFocus)
        {
            Tx(ViscaCommands.Focus(cam.ViscaAddress, dir));
            _sentFocus = dir;
            _stopPendingFocus = dir == 0 ? 2 : 0;
        }
        else if (dir == 0 && _stopPendingFocus > 0)
        {
            Tx(ViscaCommands.Focus(cam.ViscaAddress, 0)); // UDP não garante entrega: um foco "preso" é pior que um pacote a mais
            _stopPendingFocus--;
        }
    }

    // Repete as paradas pendentes enquanto a trava de emergência aguarda o neutro.
    private void Settle(CameraConfig cam, DateTime now)
    {
        if (_stopPendingPt > 0) { Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress)); _stopPendingPt--; }
        if (_stopPendingZoom > 0) { Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0)); _stopPendingZoom--; }
        if (_stopPendingFocus > 0) { Tx(ViscaCommands.Focus(cam.ViscaAddress, 0)); _stopPendingFocus--; }
    }

    /// <summary>Para tudo imediatamente, sem trava.</summary>
    public void StopAll(string reason)
    {
        lock (_lock) StopAllLocked(reason);
    }

    /// <summary>Parada de emergência: prioridade máxima. Só volta a mover depois que todas as entradas voltarem ao neutro.</summary>
    public void EmergencyStop(string reason)
    {
        lock (_lock) EmergencyStopLocked(reason);
    }

    /// <summary>
    /// PARADA DE EMERGÊNCIA (trava dura). Ordem: liga a trava (controlador e cliente VISCA) → cancela tudo →
    /// envia as paradas de Pan/Tilt, Zoom e Foco (repetidas, pois UDP não garante entrega).
    /// Só o <see cref="TryResume"/> (botão RETOMAR CONTROLE) libera. Se a rede estiver fora, a câmera pode não receber a parada.
    /// </summary>
    public void ActivateEmergency(string reason)
    {
        lock (_lock)
        {
            if (_emergency) return;
            _emergency = true;
            if (_lockable != null) _lockable.MotionLocked = true;   // 1º: nada que mova a câmera sai depois deste ponto
            EmergencyStopLocked(reason);
            Message?.Invoke("EMERGÊNCIA ATIVADA: " + reason);
        }
    }

    /// <summary>Reenvia as paradas (ex.: quando a câmera volta a responder durante a emergência). Não libera a trava.</summary>
    public void ReassertStop()
    {
        lock (_lock)
        {
            if (!_emergency) return;
            var cam = _cam();
            Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress));
            Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0));
            Tx(ViscaCommands.Focus(cam.ViscaAddress, 0));
            _stopPendingPt = _stopPendingZoom = _stopPendingFocus = 2;
        }
    }

    /// <summary>
    /// RETOMAR CONTROLE. Só libera se a câmera estiver conectada e todas as entradas em repouso.
    /// Descarta qualquer comando antigo (nada é retomado automaticamente).
    /// </summary>
    public bool TryResume(bool cameraConnected, bool inputsNeutral, out string reason)
    {
        lock (_lock)
        {
            reason = "";
            if (!_emergency) return true;
            if (!cameraConnected)
            {
                reason = "A câmera está desconectada. O controle continua BLOQUEADO até a conexão voltar; depois clique em RETOMAR CONTROLE de novo.";
                return false;
            }
            if (!inputsNeutral)
            {
                reason = "Leve os analógicos ao centro, solte todos os botões (e o joystick/zoom da tela) e clique em RETOMAR CONTROLE de novo.";
                return false;
            }
            // cancela tudo o que ainda pudesse existir
            _ax = _ay = _az = 0;
            _digPan = _digTilt = _digZoom = _digFocus = 0;
            _pulse = null; _focusPulse = null;
            _motion.Reset();
            var cam = _cam();
            Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress));       // ainda travado: só paradas passam
            Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0));
            Tx(ViscaCommands.Focus(cam.ViscaAddress, 0));
            _sentPan = _sentTilt = _sentZoom = _sentFocus = 0;
            _stopPendingPt = _stopPendingZoom = _stopPendingFocus = 0;
            _requireNeutral = false;
            _lastInput = DateTime.MinValue;
            _emergency = false;
            if (_lockable != null) _lockable.MotionLocked = false;
            Message?.Invoke("Controle retomado.");
            return true;
        }
    }

    private void StopAllLocked(string reason)
    {
        var cam = _cam();
        _ax = _ay = _az = 0;
        _pulse = null;
        _focusPulse = null;
        _motion.Reset();
        Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress));
        Tx(ViscaCommands.Zoom(cam.ViscaAddress, 0));
        Tx(ViscaCommands.Focus(cam.ViscaAddress, 0));
        _sentPan = _sentTilt = _sentZoom = _sentFocus = 0;
        _stopPendingPt = _stopPendingZoom = _stopPendingFocus = 2;
        Message?.Invoke("Parado: " + reason);
    }

    private void EmergencyStopLocked(string reason)
    {
        StopAllLocked(reason);
        var cam = _cam();
        Tx(ViscaCommands.PanTiltStop(cam.ViscaAddress)); // repetição deliberada: UDP não garante entrega
        _requireNeutral = true;
        Message?.Invoke("EMERGÊNCIA: " + reason);
    }

    public void SavePreset(int slot) { lock (_lock) { if (_emergency) return; Tx(ViscaCommands.PresetSet(_cam().ViscaAddress, slot)); } }

    public void RecallPreset(int slot)
    {
        lock (_lock)
        {
            if (_emergency) return;
            StopAllLocked("antes de acionar preset");
            Tx(ViscaCommands.PresetRecall(_cam().ViscaAddress, slot));
        }
    }

    public void Home()
    {
        lock (_lock) { if (_emergency) return; StopAllLocked("antes de ir ao Home"); Tx(ViscaCommands.Home(_cam().ViscaAddress)); }
    }

    public void ToggleAutoFocus()
    {
        lock (_lock) { if (_emergency) return; _autoFocus = !_autoFocus; Tx(ViscaCommands.AutoFocus(_cam().ViscaAddress, _autoFocus)); }
    }

    public bool AutoFocusOn => _autoFocus;

    public void CycleSpeedLevel()
    {
        var n = Math.Max(1, _profile().Motion.SpeedLevels.Length);
        SpeedLevelIndex = (SpeedLevelIndex + 1) % n;
    }
}
