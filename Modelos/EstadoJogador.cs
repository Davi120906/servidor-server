using System.Numerics;
using System.Text.Json.Serialization;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa o estado de um jogador específico
    /// Contém posição, pontuação, vidas e status
    /// </summary>
    public class EstadoJogador
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("nome")]
        public string Nome { get; set; } = "";
        
        [JsonPropertyName("posicao")]
        public Vector2 Posicao { get; set; }
        
        [JsonPropertyName("pontuacao")]
        public int Pontuacao { get; set; }
        
        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; } = true;
        
        [JsonPropertyName("vidas")]
        public int Vidas { get; set; } = 3;

        // Propriedades não enviadas pela rede (apenas servidor)
        [JsonIgnore]
        public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;
        
        [JsonIgnore]
        public DateTime UltimoTiro { get; set; } = DateTime.MinValue;
        
        [JsonIgnore]
        public DateTime UltimaDano { get; set; } = DateTime.MinValue;
        
        [JsonIgnore]
        public Vector2 PosicaoAnterior { get; set; }

        [JsonIgnore]
        public int TirosDisparados { get; set; }

        [JsonIgnore]
        public int AsteroidesDestruidos { get; set; }

        [JsonIgnore]
        public TimeSpan TempoVivo => DateTime.Now - CriadoEm;

        [JsonIgnore]
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public EstadoJogador() { }

        /// <summary>
        /// Construtor com parâmetros básicos
        /// </summary>
        public EstadoJogador(int id, string nome, Vector2 posicaoInicial)
        {
            Id = id;
            Nome = nome;
            Posicao = posicaoInicial;
            PosicaoAnterior = posicaoInicial;
            UltimaAtualizacao = DateTime.Now;
            CriadoEm = DateTime.Now;
        }

        /// <summary>
        /// Atualiza posição do jogador com validação
        /// </summary>
        public void AtualizarPosicao(Vector2 novaPosicao)
        {
            PosicaoAnterior = Posicao;
            Posicao = novaPosicao;
            UltimaAtualizacao = DateTime.Now;
        }

        /// <summary>
        /// Verifica se o jogador pode atirar (cooldown)
        /// </summary>
        public bool PodeAtirar()
        {
            var tempoDesdeUltimoTiro = DateTime.Now - UltimoTiro;
            return tempoDesdeUltimoTiro.TotalMilliseconds >= 250; // 250ms cooldown
        }

        /// <summary>
        /// Registra que o jogador atirou
        /// </summary>
        public void RegistrarTiro()
        {
            UltimoTiro = DateTime.Now;
            TirosDisparados++;
        }

        /// <summary>
        /// Verifica se o jogador está invencível (após levar dano)
        /// </summary>
        public bool EstaInvencivel()
        {
            var tempoDesdeUltimoDano = DateTime.Now - UltimaDano;
            return tempoDesdeUltimoDano.TotalMilliseconds < 2000; // 2 segundos invencível
        }

        /// <summary>
        /// Aplica dano ao jogador
        /// </summary>
        public void ReceberDano()
        {
            if (EstaInvencivel()) return;

            Vidas--;
            UltimaDano = DateTime.Now;

            if (Vidas <= 0)
            {
                Ativo = false;
            }
        }

        /// <summary>
        /// Adiciona pontuação
        /// </summary>
        public void AdicionarPontuacao(int pontos)
        {
            Pontuacao += pontos;
            
            // Vida extra a cada 10000 pontos
            if (Pontuacao > 0 && Pontuacao % 10000 == 0 && Vidas < 5)
            {
                Vidas++;
            }
        }

        /// <summary>
        /// Registra destruição de asteroide
        /// </summary>
        public void RegistrarAsteroideDestruido()
        {
            AsteroidesDestruidos++;
        }

        /// <summary>
        /// Reposiciona jogador (respawn)
        /// </summary>
        public void Reposicionar(Vector2 novaPosicao)
        {
            Posicao = novaPosicao;
            PosicaoAnterior = novaPosicao;
            UltimaAtualizacao = DateTime.Now;
        }

        /// <summary>
        /// Verifica se o jogador está inativo há muito tempo
        /// </summary>
        public bool EstaInativo(TimeSpan timeout)
        {
            return DateTime.Now - UltimaAtualizacao > timeout;
        }

        /// <summary>
        /// Calcula velocidade atual do jogador
        /// </summary>
        public float CalcularVelocidade()
        {
            var distancia = Vector2.Distance(Posicao, PosicaoAnterior);
            var tempo = (float)(DateTime.Now - UltimaAtualizacao).TotalSeconds;
            return tempo > 0 ? distancia / tempo : 0f;
        }

        /// <summary>
        /// Estatísticas do jogador
        /// </summary>
        public string ObterEstatisticas()
        {
            var tempoVivo = TempoVivo.TotalMinutes;
            var eficiencia = TirosDisparados > 0 ? (float)AsteroidesDestruidos / TirosDisparados * 100 : 0;
            
            return $"{Nome}: {Pontuacao} pts, {Vidas} vidas, " +
                   $"{AsteroidesDestruidos} kills, {eficiencia:F1}% eficiência, " +
                   $"{tempoVivo:F1}min vivo";
        }

        /// <summary>
        /// Representação string para logs
        /// </summary>
        public override string ToString()
        {
            var status = Ativo ? "VIVO" : "MORTO";
            return $"Jogador[{Id}] {Nome}: {Pontuacao} pts, {Vidas} vidas, {status}";
        }

        /// <summary>
        /// Cria cópia do jogador
        /// </summary>
        public EstadoJogador Clone()
        {
            return new EstadoJogador
            {
                Id = Id,
                Nome = Nome,
                Posicao = Posicao,
                Pontuacao = Pontuacao,
                Ativo = Ativo,
                Vidas = Vidas,
                UltimaAtualizacao = UltimaAtualizacao,
                UltimoTiro = UltimoTiro,
                UltimaDano = UltimaDano,
                PosicaoAnterior = PosicaoAnterior,
                TirosDisparados = TirosDisparados,
                AsteroidesDestruidos = AsteroidesDestruidos,
                CriadoEm = CriadoEm
            };
        }
    }
}