using System.Text.Json.Serialization;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa o estado completo do jogo enviado aos clientes
    /// Contém todos os jogadores, asteroides e tiros ativos
    /// </summary>
    public class EstadoJogo
    {
        [JsonPropertyName("jogadores")]
        public List<EstadoJogador> Jogadores { get; set; } = new();
        
        [JsonPropertyName("asteroides")]
        public List<AsteroideRede> Asteroides { get; set; } = new();
        
        [JsonPropertyName("tiros")]
        public List<TiroRede> Tiros { get; set; } = new();
        
        [JsonPropertyName("frameCount")]
        public long FrameCount { get; set; }
        
        [JsonPropertyName("jogoAtivo")]
        public bool JogoAtivo { get; set; } = true;

        [JsonPropertyName("tempoJogo")]
        public TimeSpan TempoJogo { get; set; }

        [JsonPropertyName("numeroWave")]
        public int NumeroWave { get; set; } = 1;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public EstadoJogo() { }

        /// <summary>
        /// Cria cópia do estado atual
        /// </summary>
        public EstadoJogo Clone()
        {
            return new EstadoJogo
            {
                Jogadores = new List<EstadoJogador>(Jogadores),
                Asteroides = new List<AsteroideRede>(Asteroides),
                Tiros = new List<TiroRede>(Tiros),
                FrameCount = FrameCount,
                JogoAtivo = JogoAtivo,
                TempoJogo = TempoJogo,
                NumeroWave = NumeroWave
            };
        }

        /// <summary>
        /// Obtém jogador por ID
        /// </summary>
        public EstadoJogador? ObterJogador(int id)
        {
            return Jogadores.FirstOrDefault(j => j.Id == id);
        }

        /// <summary>
        /// Obtém jogadores ativos
        /// </summary>
        public List<EstadoJogador> JogadoresAtivos()
        {
            return Jogadores.Where(j => j.Ativo).ToList();
        }

        /// <summary>
        /// Conta total de objetos no jogo
        /// </summary>
        public int TotalObjetos()
        {
            return Jogadores.Count + Asteroides.Count + Tiros.Count;
        }

        /// <summary>
        /// Verifica se o jogo tem jogadores
        /// </summary>
        public bool TemJogadores()
        {
            return Jogadores.Any();
        }

        /// <summary>
        /// Verifica se todos os jogadores foram eliminados
        /// </summary>
        public bool TodosJogadoresEliminados()
        {
            return Jogadores.Any() && !Jogadores.Any(j => j.Ativo);
        }

        /// <summary>
        /// Estatísticas para debug
        /// </summary>
        public string ObterEstatisticas()
        {
            var jogadoresAtivos = JogadoresAtivos().Count;
            var pontuacaoMaxima = Jogadores.Any() ? Jogadores.Max(j => j.Pontuacao) : 0;
            
            return $"Frame {FrameCount} | Jogadores: {jogadoresAtivos}/{Jogadores.Count} | " +
                   $"Asteroides: {Asteroides.Count} | Tiros: {Tiros.Count} | " +
                   $"Wave: {NumeroWave} | Max Pontos: {pontuacaoMaxima}";
        }

        /// <summary>
        /// Representação string para logs
        /// </summary>
        public override string ToString()
        {
            return $"EstadoJogo[Frame:{FrameCount}, Jogadores:{Jogadores.Count}, " +
                   $"Asteroides:{Asteroides.Count}, Tiros:{Tiros.Count}]";
        }
    }
}