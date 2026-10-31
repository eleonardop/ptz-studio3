# PTZ Studio — Manual (português)

## Instalação
1. Execute **PTZStudio-Setup.exe** (ou abra direto o **PTZStudio.exe**, que não precisa de instalação).
2. Não é necessário instalar Python, .NET ou qualquer outra ferramenta.

## Primeiros passos
1. Aba **Câmera**: informe o IP da câmera (LJ510-30X-N), a porta UDP e o endereço VISCA. Escolha o protocolo (UDP puro ou VISCA over IP com cabeçalho). Se a câmera não responder, tente o outro protocolo e confira a porta no manual ou no menu de rede da câmera.
2. Conecte o GameSir (Bluetooth ou USB). O topo da janela mostra o nome do controle quando ele é detectado.
3. Aba **Controle PTZ**: teste com o joystick virtual (mouse) e com o controle físico.
4. Aba **Analógicos**: ative **Pan/Tilt completo (Analógico)** e use **Calibrar automaticamente** para conferir a direção.

## Cabeçalho (sempre visível)
Mostra o estado da câmera, do controle, o perfil e a entrada ativa, e tem o botão vermelho **PARADA DE EMERGÊNCIA**.

## Parada de emergência e retomada
- **PARADA DE EMERGÊNCIA** (botão vermelho no topo, ou o botão do controle que você ligar a essa função) para Pan, Tilt, Zoom e Foco e **trava tudo**: controle físico, joystick e zoom da tela, botões e presets.
- Enquanto estiver ativa, aparece a faixa vermelha **EMERGÊNCIA ATIVADA**. A única forma de liberar é o botão **RETOMAR CONTROLE**.
- Para retomar é preciso: câmera conectada, analógicos no centro, nenhum botão pressionado e joystick/zoom da tela soltos. Se algo faltar, a faixa explica o quê.
- Se a conexão com a câmera cair durante a emergência, o bloqueio **continua** mesmo quando ela voltar; é preciso clicar em RETOMAR CONTROLE de novo.
- A trava existe dentro da lógica de envio (no controlador e no cliente VISCA), não só na tela: nenhum comando de movimento sai enquanto ela estiver ativa.
- **Atenção:** a parada depende da rede. Se a rede ou a câmera estiverem fora, o programa não consegue garantir que a câmera recebeu a parada. Em caso de risco, use também o controle remoto da câmera ou desligue-a.

## Pan/Tilt completo (Analógico)
Aba **Analógicos**. Um único analógico controla Pan e Tilt ao mesmo tempo: cima/baixo = Tilt, esquerda/direita = Pan, diagonais movem os dois, centro = para. Quanto mais inclinado, mais rápido, dentro dos limites que você definir. Ajustes: zona morta, sensibilidade, velocidade máxima de Pan e de Tilt (independentes), inversão dos eixos X e Y, aceleração e desaceleração. As opções separadas de Pan e Tilt continuam nas abas Analógicos e Sensibilidade.

## Mapeamento do controle
- Escolha **Frente** ou **Traseira**, clique no botão do desenho, escolha a função e clique em **Aplicar**.
- O desenho foi traçado a partir das fotos do GameSir Nova 2 Lite: analógicos, direcional, ABXY, LB/RB, LT/RT, View, Menu, Home, Screenshot, M e, na traseira, L4 e R4.
- Botões com função mostram um **selo azul** e uma etiqueta com o nome da função; botões pressionados acendem em verde; o ponto azul mostra a posição do analógico.
- **DETECTAR BOTÃO**: selecione o botão no desenho, clique em DETECTAR BOTÃO e aperte o botão físico. Faça isso para M, Screenshot, Home, L4 e R4, que não têm número fixo no Windows.
- L4, R4 e M só aparecem se o Windows os enxergar no modo de conexão em uso. Se não aparecerem, veja a aba **Diagnóstico**.

## Presets
O preset é gravado na **memória da câmera** por comando VISCA. O programa só marca como confirmado quando a câmera responde; antes disso aparece como “aguardando confirmação”. Durante a emergência, gravar, acionar e excluir presets ficam bloqueados.

## Ao lado do relógio (bandeja do Windows)
- Ao **minimizar**, o PTZ Studio some da barra de tarefas e fica como um ícone ao lado do relógio. **Tudo continua funcionando**: controle físico, joystick virtual, presets, reconexão e parada de emergência.
- **Clique no ícone** para abrir de novo. Botão direito: Abrir, **PARADA DE EMERGÊNCIA**, **RETOMAR CONTROLE** e Sair. Passar o mouse mostra o estado da câmera e do controle.
- O botão **fechar (X)** também só esconde a janela, para evitar fechar sem querer durante a transmissão. Para sair de verdade: botão direito no ícone > **Sair do PTZ Studio**.
- Com a janela escondida, o programa avisa por um balão quando a emergência é acionada ou a câmera perde a conexão.
- Abrir o programa de novo enquanto ele está na bandeja apenas traz a janela original (só uma cópia pode controlar a câmera).
- Para mudar esse comportamento: aba **Backup > Janela e bandeja do Windows**.

## Desempenho
O programa foi feito para ser leve: com a janela escondida ou minimizada, as telas param de se atualizar; o controle dos movimentos tem prioridade sobre o desenho da tela; o processo roda com prioridade um pouco acima do normal (sem atrapalhar o OBS) e fora do “modo de economia” do Windows 11, que deixaria os movimentos irregulares quando minimizado.

## Registros
O programa **não** mostra nem guarda histórico de comandos (TX/RX) e não grava arquivos de registro contínuos. Na aba **Diagnóstico** ficam apenas os últimos erros de comunicação (na memória) e o botão **Copiar diagnóstico**, para pedir ajuda. Só falhas inesperadas do próprio programa vão para um pequeno arquivo `erros.log` (limite de 200 KB).

## Backup
Aba **Backup**: criar backup, restaurar, exportar e importar configurações. Um backup automático é feito ao abrir o programa.

## Onde ficam os dados
Pasta `%AppData%\PTZStudio` (configurações e backups). Suas configurações antigas continuam valendo: as novas opções entram com valores que não mudam o comportamento anterior.
