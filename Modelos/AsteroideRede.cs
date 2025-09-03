using System.Numerics;
using System.Text.Json.Serialization;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa um asteroide para transmissão via rede
    /// Versão simplificada do AsteroideServidor, contém apenas dados essenciais
    /// </summary>
    public class AsteroideRede
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("posicao")]
        public Vector2 Posicao { get; set; }
        
        [JsonPropertyName("velocidade")]
        public Vector2 Velocidade { get; set; }
        
        [JsonPropertyName("raio")]
        public float Raio { get; set; }

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public AsteroideRede() { }

        /// <summary>
        /// Construtor com parâmetros
        /// </summary>
        public AsteroideRede(int id, Vector2 posicao, Vector2 velocidade, float raio)
        {
            Id = id;
            Posicao = posicao;
            Velocidade = velocidade;
            Raio = raio;
        }

        /// <summary>
        /// Cria a partir de AsteroideServidor
        /// </summary>
        public static AsteroideRede DeServidor(AsteroideServidor asteroideServidor)
        {
            return new AsteroideRede
            {
                Id = asteroideServidor.Id,
                Posicao = asteroideServidor.Posicao,
                Velocidade = asteroideServidor.Velocidade,
                Raio = asteroideServidor.Raio
            };
        }

        /// <summary>
        /// Verifica se o asteroide colidiu com um ponto
        /// </summary>
        public bool ColidiuCom(Vector2 ponto, float raioExtra = 0f)
        {
            var distancia = Vector2.Distance(Posicao, ponto);
            return distancia <= (Raio + raioExtra);
        }

        /// <summary>
        /// Calcula a área do asteroide
        /// </summary>
        public float Area => (float)(Math.PI * Raio * Raio);

        /// <summary>
        /// Determina o tipo do asteroide baseado no tamanho
        /// </summary>
        public string TipoAsteroide
        {
            get
            {
                if (Raio > 40) return "GRANDE";
                if (Raio > 25) return "MEDIO";
                return "PEQUENO";
            }
        }

        /// <summary>
        /// Calcula pontuação que o asteroide vale
        /// </summary>
        public int Pontuacao
        {
            get
            {
                return TipoAsteroide switch
                {
                    "GRANDE" => 20,
                    "MEDIO" => 50,
                    "PEQUENO" => 100,
                    _ => 10
                };
            }
        }

        /// <summary>
        /// Representação string para debug
        /// </summary>
        public override string ToString()
        {
            return $"Asteroide[{Id}] {TipoAsteroide} em ({Posicao.X:F1}, {Posicao.Y:F1}) " +
                   $"vel({Velocidade.X:F1}, {Velocidade.Y:F1}) r={Raio:F1}";
        }

        /// <summary>
        /// Cria cópia do asteroide
        /// </summary>
        public AsteroideRede Clone()
        {
            return new AsteroideRede
            {
                Id = Id,
                Posicao = Posicao,
                Velocidade = Velocidade,
                Raio = Raio
            };
        }

        /// <summary>
        /// Verifica igualdade baseada no ID
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is AsteroideRede other && Id == other.Id;
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