using System.Text.Json.Serialization;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa uma mensagem trocada entre cliente e servidor via TCP
    /// Deve ser idêntica à classe do cliente para compatibilidade
    /// </summary>
    public class MensagemRede
    {
        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "";
        
        [JsonPropertyName("dados")]
        public string Dados { get; set; } = "";
        
        [JsonPropertyName("jogadorId")]
        public int JogadorId { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public MensagemRede() { }

        /// <summary>
        /// Construtor com parâmetros
        /// </summary>
        public MensagemRede(string tipo, string dados, int jogadorId = -1)
        {
            Tipo = tipo;
            Dados = dados;
            JogadorId = jogadorId;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Cria mensagem de erro
        /// </summary>
        public static MensagemRede CriarErro(string mensagem, int jogadorId = -1)
        {
            return new MensagemRede("ERRO", mensagem, jogadorId);
        }

        /// <summary>
        /// Cria mensagem de ping
        /// </summary>
        public static MensagemRede CriarPing(int jogadorId = -1)
        {
            return new MensagemRede("PING", DateTime.Now.Ticks.ToString(), jogadorId);
        }

        /// <summary>
        /// Cria mensagem de pong
        /// </summary>
        public static MensagemRede CriarPong(string dadosOriginais, int jogadorId = -1)
        {
            return new MensagemRede("PONG", dadosOriginais, jogadorId);
        }

        /// <summary>
        /// Representação string para debug
        /// </summary>
        public override string ToString()
        {
           return $"[{Tipo}] Jogador {JogadorId} - {Dados?.Substring(0, Math.Min(50, Dados.Length))}";

        }
    }
}