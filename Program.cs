using AsteroidesServidor.Rede;
using AsteroidesServidor.GameLogic;
using AsteroidesServidor.Utils;

namespace AsteroidesServidor
{
    class Program
    {
        private static ServidorTCP? _servidor;
        private static GerenciadorJogo? _gerenciadorJogo;
        private static volatile bool _executando = true;

        static async Task Main(string[] args)
        {
            Console.Title = "Servidor Asteroides Multiplayer";
            
            // Banner de inicialização
            ExibirBanner();
            
            // Configurar handlers de sinal
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _executando = false;
                Console.WriteLine("\n[SERVER] Finalizando servidor...");
            };
            
            try
            {
                // Inicializar componentes
                await InicializarServidor();
                
                // Loop principal do servidor
                await ExecutarLoopPrincipal();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Erro fatal no servidor: {ex.Message}");
                Console.WriteLine($"[ERRO] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Cleanup
                await FinalizarServidor();
            }
            
            Console.WriteLine("[SERVER] Servidor finalizado. Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        /// <summary>
        /// Inicializa todos os componentes do servidor
        /// </summary>
        private static async Task InicializarServidor()
        {
            Console.WriteLine($"[SERVER] Inicializando servidor na porta {Constantes.PORTA_PADRAO}...");
            
            // Criar gerenciador de jogo
            _gerenciadorJogo = new GerenciadorJogo();
            
            // Criar servidor TCP
            _servidor = new ServidorTCP();
            
            // Configurar eventos entre servidor e jogo
            _servidor.NovoCliente += _gerenciadorJogo.AdicionarJogador;
            _servidor.ClienteDesconectado += _gerenciadorJogo.RemoverJogador;
            _servidor.MensagemRecebida += _gerenciadorJogo.ProcessarMensagemJogador;
            
            _gerenciadorJogo.EstadoJogoAtualizado += _servidor.BroadcastEstadoJogo;
            _gerenciadorJogo.MensagemParaJogador += _servidor.EnviarMensagemPara;
            
    
            var iniciado = await _servidor.IniciarAsync();
            
            if (!iniciado)
            {
                throw new InvalidOperationException("Falha ao iniciar o servidor TCP");
            }
            
            Console.WriteLine($"[SERVER] Servidor iniciado com sucesso!");
            Console.WriteLine($"[SERVER] Aguardando conexões de clientes...");
            Console.WriteLine($"[SERVER] Pressione Ctrl+C para parar o servidor");

        }

        /// <summary>
        /// Loop principal do servidor
        /// </summary>
        private static async Task ExecutarLoopPrincipal()
        {
            var ultimoUpdate = DateTime.Now;
            var contadorFrames = 0;
            var ultimoRelatario = DateTime.Now;
            
            while (_executando)
            {
                var agora = DateTime.Now;
                var deltaTime = agora - ultimoUpdate;
                
                // Atualizar lógica do jogo (60 FPS)
                if (deltaTime.TotalMilliseconds >= Constantes.INTERVALO_FRAME)
                {
                    _gerenciadorJogo?.AtualizarJogo(deltaTime);
                    ultimoUpdate = agora;
                    contadorFrames++;
                }
                
                // Relatório de status a cada 30 segundos
                if ((agora - ultimoRelatario).TotalSeconds >= 30)
                {
                    ExibirStatusServidor(contadorFrames / 30);
                    contadorFrames = 0;
                    ultimoRelatario = agora;
                }
                
                // Sleep curto para não sobrecarregar CPU
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Finaliza todos os componentes do servidor
        /// </summary>
        private static async Task FinalizarServidor()
        {
            Console.WriteLine("[SERVER] Finalizando componentes...");
            
            // Parar o jogo
            _gerenciadorJogo?.Finalizar();
            
            // Fechar servidor
            if (_servidor != null)
            {
                await _servidor.PararAsync();
            }
            
            Console.WriteLine("[SERVER] Todos os componentes finalizados");
        }

        /// <summary>
        /// Exibe banner de inicialização
        /// </summary>
        private static void ExibirBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ASTEROIDES MULTIPLAYER SERVER              ║");
            Console.WriteLine("║                                                          ║");
            Console.WriteLine("║  Servidor TCP com paralelismo para múltiplos jogadores  ║");
            Console.WriteLine("║  Implementa async/await e programação paralela          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.ResetColor();
        }

        /// <summary>
        /// Exibe status atual do servidor
        /// </summary>
        private static void ExibirStatusServidor(int fps)
        {
            var clientes = _servidor?.ClientesConectados ?? 0;
            var jogadores = _gerenciadorJogo?.NumeroJogadores ?? 0;
            var asteroides = _gerenciadorJogo?.NumeroAsteroides ?? 0;
            var tiros = _gerenciadorJogo?.NumeroTiros ?? 0;
            var memoria = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            
            Console.WriteLine($"[STATUS] Clientes: {clientes} | Jogadores: {jogadores} | " +
                            $"Asteroides: {asteroides} | Tiros: {tiros} | " +
                            $"FPS: {fps} | Mem: {memoria}MB");
        }
    }
}