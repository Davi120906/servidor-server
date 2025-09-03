using System.Numerics;
using System.Text.Json.Serialization;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa um tiro para transmissão via rede
    /// Versão simplificada do TiroServidor, contém apenas dados essenciais
    /// </summary>
    public class TiroRede
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("posicao")]
        public Vector2 Posicao { get; set; }
        
        [JsonPropertyName("velocidade")]
        public Vector2 Velocidade { get; set; }
        
        [JsonPropertyName("jogadorId")]
        public int JogadorId { get; set; }

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public TiroRede() { }

        /// <summary>
        /// Construtor com parâmetros
        /// </summary>
        public TiroRede(int id, Vector2 posicao, Vector2 velocidade, int jogadorId)
        {
            Id = id;
            Posicao = posicao;
            Velocidade = velocidade;
            JogadorId = jogadorId;
        }

        /// <summary>
        /// Cria a partir de TiroServidor
        /// </summary>
        public static TiroRede DeServidor(TiroServidor tiroServidor)
        {
            return new TiroRede
            {
                Id = tiroServidor.Id,
                Posicao = tiroServidor.Posicao,
                Velocidade = tiroServidor.Velocidade,
                JogadorId = tiroServidor.JogadorId
            };
        }

        /// <summary>
        /// Verifica se o tiro colidiu com um ponto
        /// </summary>
        public bool ColidiuCom(Vector2 ponto, float raio = 3f)
        {
            var distancia = Vector2.Distance(Posicao, ponto);
            return distancia <= raio;
        }

        /// <summary>
        /// Calcula a distância percorrida baseada na velocidade
        /// </summary>
        public float DistanciaPorFrame => Velocidade.Length();

        /// <summary>
        /// Verifica se é um tiro válido (velocidade não zero)
        /// </summary>
        public bool EhValido => Velocidade.Length() > 0.1f;

        /// <summary>
        /// Direção normalizada do tiro
        /// </summary>
        public Vector2 Direcao
        {
            get
            {
                var vel = Velocidade;
                if (vel.Length() > 0)
                    return Vector2.Normalize(vel);
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Prediz posição futura do tiro
        /// </summary>
        public Vector2 PosicaoFutura(float tempoSegundos)
        {
            return Posicao + Velocidade * tempoSegundos * 60f; // 60 FPS base
        }

        /// <summary>
        /// Representação string para debug
        /// </summary>
        public override string ToString()
        {
            return $"Tiro[{Id}] do Jogador {JogadorId} em ({Posicao.X:F1}, {Posicao.Y:F1}) " +
                   $"vel({Velocidade.X:F1}, {Velocidade.Y:F1})";
        }

        /// <summary>
        /// Cria cópia do tiro
        /// </summary>
        public TiroRede Clone()
        {
            return new TiroRede
            {
                Id = Id,
                Posicao = Posicao,
                Velocidade = Velocidade,
                JogadorId = JogadorId
            };
        }

        /// <summary>
        /// Verifica igualdade baseada no ID
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is TiroRede other && Id == other.Id;
        }

        /// <summary>
        /// Hash code baseado no ID
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}