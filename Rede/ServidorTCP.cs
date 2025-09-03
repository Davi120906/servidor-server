using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using AsteroidesServidor.Modelos;
using AsteroidesServidor.Utils;

namespace AsteroidesServidor.Rede
{
    /// <summary>
    /// Servidor TCP que gerencia múltiplos clientes usando paralelismo (REQUISITO)
    /// Implementa async/await para comunicação não-bloqueante (REQUISITO)
    /// </summary>
    public class ServidorTCP
    {
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<int, ClienteConectado> _clientes = new();
        private CancellationTokenSource _cancelToken = new();
        private int _proximoIdJogador = 1;
        
        // Eventos
        public event Action<ClienteConectado>? NovoCliente;
        public event Action<int>? ClienteDesconectado;
        public event Action<int, MensagemRede>? MensagemRecebida;

        public int ClientesConectados => _clientes.Count;

        /// <summary>
        /// Inicia o servidor TCP de forma assíncrona (REQUISITO: async/await)
        /// </summary>
        public async Task<bool> IniciarAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, Constantes.PORTA_PADRAO);
                _listener.Start();
                
                Console.WriteLine($"[TCP] Servidor TCP iniciado na porta {Constantes.PORTA_PADRAO}");
                
                // Iniciar aceitação de clientes em paralelo (REQUISITO: paralelismo)
                _ = Task.Run(AceitarClientesAsync);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Erro ao iniciar servidor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loop para aceitar novos clientes (REQUISITO: async/await e paralelismo)
        /// </summary>
        private async Task AceitarClientesAsync()
        {
            if (_listener == null) return;
            
            Console.WriteLine("[TCP] Aguardando conexões de clientes...");
            
            try
            {
                while (!_cancelToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Aceitar cliente de forma assíncrona
                        var tcpClient = await _listener.AcceptTcpClientAsync();
                        
                        // Processar cliente em paralelo (REQUISITO: não bloquear)
                        _ = Task.Run(() => ProcessarNovoClienteAsync(tcpClient));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener foi fechado - finalização normal
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TCP] Erro ao aceitar cliente: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Erro no loop de aceitação: {ex.Message}");
            }
            
            Console.WriteLine("[TCP] Loop de aceitação finalizado");
        }

        /// <summary>
        /// Processa novo cliente conectado (REQUISITO: async/await)
        /// </summary>
        private async Task ProcessarNovoClienteAsync(TcpClient tcpClient)
        {
            var enderecoCliente = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Desconhecido";
            var idJogador = Interlocked.Increment(ref _proximoIdJogador);
            
            Console.WriteLine($"[TCP] Novo cliente conectado: {enderecoCliente} (ID: {idJogador})");
            
            try
            {
                var cliente = new ClienteConectado(idJogador, tcpClient);
                _clientes[idJogador] = cliente;
                
                // Notificar novo cliente
                NovoCliente?.Invoke(cliente);
                
                // Enviar ID do jogador
                await cliente.EnviarMensagemAsync(new MensagemRede
                {
                    Tipo = Constantes.TipoMensagem.ID_JOGADOR,
                    Dados = idJogador.ToString(),
                    JogadorId = idJogador
                });
                
                // Iniciar loop de recepção em paralelo (REQUISITO: paralelismo)
                await ReceberMensagensClienteAsync(cliente);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Erro ao processar cliente {idJogador}: {ex.Message}");
            }
            finally
            {
                // Cleanup quando cliente desconecta
                _clientes.TryRemove(idJogador, out _);
                ClienteDesconectado?.Invoke(idJogador);
                
                try
                {
                    tcpClient.Close();
                }
                catch { }
                
                Console.WriteLine($"[TCP] Cliente {idJogador} desconectado ({enderecoCliente})");
            }
        }

        /// <summary>
        /// Loop para receber mensagens do cliente (REQUISITO: async/await)
        /// </summary>
        private async Task ReceberMensagensClienteAsync(ClienteConectado cliente)
        {
            var buffer = new byte[8192];
            var stream = cliente.Stream;
            
            try
            {
                while (cliente.EstaConectado && !_cancelToken.Token.IsCancellationRequested)
                {
                    var bytesLidos = await stream.ReadAsync(buffer, 0, buffer.Length, _cancelToken.Token);
                    
                    if (bytesLidos == 0)
                    {
                        // Cliente desconectou
                        break;
                    }
                    
                    var mensagemJson = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                    
                    // Pode ter múltiplas mensagens concatenadas
                    var mensagens = mensagemJson.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var msgJson in mensagens)
                    {
                        try
                        {
                            var mensagem = JsonSerializer.Deserialize<MensagemRede>(msgJson);
                            if (mensagem != null)
                            {
                                // Processar mensagem em paralelo (REQUISITO: não bloquear)
                                _ = Task.Run(() => MensagemRecebida?.Invoke(cliente.Id, mensagem));
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"[TCP] Erro ao deserializar mensagem do cliente {cliente.Id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Erro na recepção do cliente {cliente.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Envia broadcast do estado do jogo para todos os clientes (REQUISITO: paralelismo)
        /// </summary>
        public void BroadcastEstadoJogo(EstadoJogo estado)
{
    if (_clientes.IsEmpty) return;

    var mensagem = new MensagemRede
    {
        Tipo = Constantes.TipoMensagem.ESTADO_JOGO,
        Dados = JsonSerializer.Serialize(estado),
        Timestamp = DateTime.Now
    };

    var tarefasEnvio = _clientes.Values
        .Where(c => c.EstaConectado)
        .Select(cliente => cliente.EnviarMensagemAsync(mensagem));

    try
    {
        Task.WhenAll(tarefasEnvio).Wait(TimeSpan.FromMilliseconds(100));
    }
    catch (TimeoutException)
    {
        Console.WriteLine("[TCP] Timeout no broadcast - alguns clientes podem estar lentos");
    }
}


        /// <summary>
        /// Envia mensagem para um cliente específico
        /// </summary>
        public async Task EnviarMensagemPara(int jogadorId, MensagemRede mensagem)
        {
            if (_clientes.TryGetValue(jogadorId, out var cliente))
            {
                try
                {
                    await cliente.EnviarMensagemAsync(mensagem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP] Erro ao enviar mensagem para cliente {jogadorId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Para o servidor e desconecta todos os clientes (REQUISITO: robustez)
        /// </summary>
        public async Task PararAsync()
        {
            Console.WriteLine("[TCP] Parando servidor TCP...");
            
            _cancelToken.Cancel();
            
            // Desconectar todos os clientes
            var tarefasDesconexao = _clientes.Values.Select(cliente => Task.Run(() =>
            {
                try
                {
                    cliente.Desconectar();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP] Erro ao desconectar cliente {cliente.Id}: {ex.Message}");
                }
            }));
            
            await Task.WhenAll(tarefasDesconexao);
            
            // Parar listener
            try
            {
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP] Erro ao parar listener: {ex.Message}");
            }
            
            _clientes.Clear();
            Console.WriteLine("[TCP] Servidor TCP parado");
        }

        /// <summary>
        /// Envia ping para todos os clientes conectados (para manter conexão ativa)
        /// </summary>
        /// 
        
        public async Task EnviarPingParaTodos()
        {
            var mensagem = new MensagemRede
            {
                Tipo = Constantes.TipoMensagem.PING,
                Dados = DateTime.Now.Ticks.ToString(),
                Timestamp = DateTime.Now
            };
            
            var tarefasPing = _clientes.Values
                .Where(c => c.EstaConectado)
                .Select(cliente => cliente.EnviarMensagemAsync(mensagem));
            
            try
            {
                await Task.WhenAll(tarefasPing);
            }
            catch
            {
                // Ignorar erros de ping - clientes problemáticos serão removidos
            }
        }
    }
}