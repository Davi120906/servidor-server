using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AsteroidesServidor.Modelos;

namespace AsteroidesServidor.Rede
{
    /// <summary>
    /// Representa um cliente conectado ao servidor
    /// Gerencia comunicação TCP com um jogador específico
    /// </summary>
    public class ClienteConectado
    {
        private readonly TcpClient _cliente;
        private readonly NetworkStream _stream;
        private readonly object _envioLock = new();
        
        public int Id { get; }
        public NetworkStream Stream => _stream;
        public bool EstaConectado => _cliente.Connected;
        public string EnderecoRemoto { get; }
        public DateTime HoraConexao { get; }
        public DateTime UltimaAtividade { get; private set; }

        public ClienteConectado(int id, TcpClient cliente)
        {
            Id = id;
            _cliente = cliente;
            _stream = cliente.GetStream();
            EnderecoRemoto = cliente.Client.RemoteEndPoint?.ToString() ?? "Desconhecido";
            HoraConexao = DateTime.Now;
            UltimaAtividade = DateTime.Now;
        }

        /// <summary>
        /// Envia mensagem para o cliente de forma thread-safe
        /// </summary>
        public async Task EnviarMensagemAsync(MensagemRede mensagem)
        {
            if (!EstaConectado) 
                throw new InvalidOperationException($"Cliente {Id} não está conectado");
            
            try
            {
                mensagem.Timestamp = DateTime.Now;
                
                var json = JsonSerializer.Serialize(mensagem) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);
                
                // Lock para evitar concorrência no envio
                lock (_envioLock)
                {
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
                
                UltimaAtividade = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Erro ao enviar mensagem para cliente {Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Atualiza timestamp da última atividade
        /// </summary>
        public void AtualizarAtividade()
        {
            UltimaAtividade = DateTime.Now;
        }

        /// <summary>
        /// Verifica se o cliente está inativo há muito tempo
        /// </summary>
        public bool EstaInativo(TimeSpan timeout)
        {
            return DateTime.Now - UltimaAtividade > timeout;
        }

        /// <summary>
        /// Desconecta o cliente
        /// </summary>
        public void Desconectar()
        {
            try
            {
                // Enviar mensagem de desconexão se possível
                var mensagemDesconexao = new MensagemRede
                {
                    Tipo = "SERVIDOR_PARANDO",
                    Dados = "Servidor está sendo finalizado",
                    JogadorId = -1
                };
                
                try
                {
                    var json = JsonSerializer.Serialize(mensagemDesconexao) + "\n";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
                catch
                {
                    // Ignorar erros ao enviar mensagem de desconexão
                }
                
                _stream.Close();
                _cliente.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT] Erro ao desconectar cliente {Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Informações do cliente para debug
        /// </summary>
        public override string ToString()
        {
            var tempoConectado = DateTime.Now - HoraConexao;
            var tempoInativo = DateTime.Now - UltimaAtividade;
            
            return $"Cliente {Id} ({EnderecoRemoto}) - " +
                   $"Conectado há {tempoConectado:hh\\:mm\\:ss}, " +
                   $"Inativo há {tempoInativo:mm\\:ss}";
        }
    }
}