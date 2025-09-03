using System.Numerics;
using System.Text.Json.Serialization;
using AsteroidesServidor.Utils;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa um asteroide no servidor com lógica completa
    /// Contém dados de jogo e métodos de atualização
    /// </summary>
    public class AsteroideServidor
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("posicao")]
        public Vector2 Posicao { get; set; }

        [JsonPropertyName("velocidade")]
        public Vector2 Velocidade { get; set; }

        [JsonPropertyName("raio")]
        public float Raio { get; set; }

        // Propriedades apenas do servidor
        [JsonIgnore]
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        [JsonIgnore]
        public float Rotacao { get; set; }

        [JsonIgnore]
        public float VelocidadeRotacao { get; set; }

        [JsonIgnore]
        public int Geracao { get; set; } = 1; // 1=original, 2=fragmento, 3=sub-fragmento

        [JsonIgnore]
        public Vector2 PosicaoAnterior { get; set; }

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public AsteroideServidor()
        {
            VelocidadeRotacao = (float)(new Random().NextDouble() - 0.5) * 4f; // -2 a +2 rad/s
        }

        /// <summary>
        /// Construtor com parâmetros
        /// </summary>
        public AsteroideServidor(int id, Vector2 posicao, Vector2 velocidade, float raio, int geracao = 1)
        {
            Id = id;
            Posicao = posicao;
            PosicaoAnterior = posicao;
            Velocidade = velocidade;
            Raio = raio;
            Geracao = geracao;
            CriadoEm = DateTime.Now;
            VelocidadeRotacao = (float)(new Random().NextDouble() - 0.5) * 4f;
        }

        /// <summary>
        /// Atualiza posição e rotação do asteroide
        /// </summary>
        public void Atualizar(float deltaTime)
        {
            PosicaoAnterior = Posicao;

            // Atualizar posição
            Posicao += Velocidade * deltaTime * 60f; // Normalizar para 60 FPS

            // Atualizar rotação
            Rotacao += VelocidadeRotacao * deltaTime;

            // Wrap around da rotação
            if (Rotacao > MathF.PI * 2) Rotacao -= MathF.PI * 2;
            if (Rotacao < 0) Rotacao += MathF.PI * 2;
        }

        /// <summary>
        /// Implementa comportamento de wrap-around nas bordas da tela
        /// </summary>
        public void AplicarWrapAround()
        {
            // Horizontal wrap
            if (Posicao.X < -Raio - 50)
                Posicao = new Vector2(Constantes.LARGURA_TELA + Raio + 50, Posicao.Y);
            else if (Posicao.X > Constantes.LARGURA_TELA + Raio + 50)
                Posicao = new Vector2(-Raio - 50, Posicao.Y);
        }

        /// <summary>
        /// Verifica se o asteroide saiu da área de jogo
        /// </summary>
        public bool EstaForaDaArea()
        {
            return Posicao.Y > Constantes.ALTURA_TELA + 100; // Apenas sai por baixo
        }

        /// <summary>
        /// Verifica colisão com outro objeto circular
        /// </summary>
        public bool ColidiuCom(Vector2 posicaoOutro, float raioOutro)
        {
            var distanciaQuadrada = Vector2.DistanceSquared(Posicao, posicaoOutro);
            var raioSomaQuadrada = (Raio + raioOutro) * (Raio + raioOutro);
            return distanciaQuadrada <= raioSomaQuadrada;
        }

        /// <summary>
        /// Cria fragmentos quando o asteroide é destruído
        /// </summary>
        public List<AsteroideServidor> CriarFragmentos(int proximoId)
        {
            var fragmentos = new List<AsteroideServidor>();

            // Só fragmenta se for grande o suficiente
            if (Raio < 20 || Geracao >= 3)
                return fragmentos;

            var random = new Random();
            var novoRaio = Raio * 0.6f;
            var numFragmentos = random.Next(2, 4); // 2-3 fragmentos

            for (int i = 0; i < numFragmentos; i++)
            {
                // Ângulo para espalhar fragmentos
                var angulo = (float)(2 * Math.PI * i / numFragmentos + (random.NextDouble() - 0.5) * 0.5);

                // Deslocamento inicial
                var deslocamento = new Vector2(
                    (float)Math.Cos(angulo) * (Raio + novoRaio),
                    (float)Math.Sin(angulo) * (Raio + novoRaio)
                );

                // Nova velocidade (mantém momentum + dispersão)
                var fatorVelocidade = 1.2f + (float)random.NextDouble() * 0.6f; // 1.2x - 1.8x
                var novaVelocidade = new Vector2(
                    Velocidade.X + (float)Math.Cos(angulo) * fatorVelocidade,
                    Velocidade.Y + (float)Math.Sin(angulo) * fatorVelocidade
                );

                var fragmento = new AsteroideServidor(
                    proximoId + i,
                    Posicao + deslocamento,
                    novaVelocidade,
                    novoRaio,
                    Geracao + 1
                );

                fragmentos.Add(fragmento);
            }

            return fragmentos;
        }

        /// <summary>
        /// Calcula pontuação baseada no tamanho
        /// </summary>
        public int CalcularPontuacao()
        {
            return TipoAsteroide switch
            {
                "GRANDE" => Constantes.PONTOS_ASTEROIDE_GRANDE,
                "MEDIO" => Constantes.PONTOS_ASTEROIDE_MEDIO,
                "PEQUENO" => Constantes.PONTOS_ASTEROIDE_PEQUENO,
                _ => 10
            };
        }

        /// <summary>
        /// Determina o tipo do asteroide baseado no tamanho
        /// </summary>
        public string TipoAsteroide
        {
            get
            {
                if (Raio > 35) return "GRANDE";
                if (Raio > 20) return "MEDIO";
                return "PEQUENO";
            }
        }

        /// <summary>
        /// Tempo de vida do asteroide
        /// </summary>
        public TimeSpan TempoVida => DateTime.Now - CriadoEm;

        /// <summary>
        /// Velocidade atual do asteroide
        /// </summary>
        public float VelocidadeAtual => Velocidade.Length();

        /// <summary>
        /// Área do asteroide (para cálculos de física)
        /// </summary>
        public float Area => (float)(Math.PI * Raio * Raio);

        /// <summary>
        /// Massa do asteroide (baseada na área)
        /// </summary>
        public float Massa => Area * 0.1f;

        /// <summary>
        /// Converte para modelo de rede
        /// </summary>
        public AsteroideRede ParaRede()
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
        /// Aplica força ao asteroide (para efeitos físicos)
        /// </summary>
        public void AplicarForca(Vector2 forca, float deltaTime)
        {
            var aceleracao = forca / Massa;
            Velocidade += aceleracao * deltaTime;

            // Limitar velocidade máxima
            var velocidadeMax = 6f;
            if (Velocidade.Length() > velocidadeMax)
            {
                Velocidade = Vector2.Normalize(Velocidade) * velocidadeMax;
            }
        }

        /// <summary>
        /// Verifica se está se movendo na direção de um ponto
        /// </summary>
        public bool EstaSeDirigindoPara(Vector2 ponto)
        {
            var direcaoParaPonto = Vector2.Normalize(ponto - Posicao);
            var direcaoVelocidade = Vector2.Normalize(Velocidade);
            var produto = Vector2.Dot(direcaoVelocidade, direcaoParaPonto);
            return produto > 0.5f; // Ângulo menor que 60 graus
        }

        /// <summary>
        /// Calcula posição futura baseada na velocidade
        /// </summary>
        public Vector2 PosicaoFutura(float tempoSegundos)
        {
            return Posicao + Velocidade * tempoSegundos * 60f;
        }

        /// <summary>
        /// Representação string para debug
        /// </summary>
        public override string ToString()
        {
            return $"Asteroide[{Id}] {TipoAsteroide} G{Geracao} em ({Posicao.X:F1}, {Posicao.Y:F1}) " +
                   $"vel({Velocidade.X:F1}, {Velocidade.Y:F1}) r={Raio:F1} " +
                   $"idade={TempoVida.TotalSeconds:F1}s";
        }

        /// <summary>
        /// Cria cópia do asteroide
        /// </summary>
        public AsteroideServidor Clone()
        {
            return new AsteroideServidor
            {
                Id = Id,
                Posicao = Posicao,
                PosicaoAnterior = PosicaoAnterior,
                Velocidade = Velocidade,
                Raio = Raio,
                Rotacao = Rotacao,
                VelocidadeRotacao = VelocidadeRotacao,
                Geracao = Geracao,
                CriadoEm = CriadoEm
            };
        }

        /// <summary>
        /// Verifica igualdade baseada no ID
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is AsteroideServidor other && Id == other.Id;
        }

        /// <summary>
        /// Hash code baseado no ID
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Cria asteroide aleatório
        /// </summary>
        public static AsteroideServidor CriarAleatorio(int id, Vector2? posicaoInicial = null)
        {
            var random = new Random();

            var posicao = posicaoInicial ?? new Vector2(
                random.Next(-50, Constantes.LARGURA_TELA + 50),
                -50
            );

            var velocidade = new Vector2(
                (float)(random.NextDouble() - 0.5) * 4f, // -2 a +2 horizontal
                (float)(random.NextDouble() * 2 + 1f)     // 1 a 3 vertical (para baixo)
            );

            var raio = (float)(random.NextDouble() *
                              (Constantes.RAIO_MAX_ASTEROIDE - Constantes.RAIO_MIN_ASTEROIDE) +
                              Constantes.RAIO_MIN_ASTEROIDE);

            return new AsteroideServidor(id, posicao, velocidade, raio);
        }
    }
}
 