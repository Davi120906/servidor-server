using System.Numerics;
using System.Text.Json.Serialization;
using AsteroidesServidor.Utils;

namespace AsteroidesServidor.Modelos
{
    /// <summary>
    /// Representa um tiro no servidor com lógica completa
    /// Contém dados de jogo e métodos de atualização
    /// </summary>
    public class TiroServidor
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("posicao")]
        public Vector2 Posicao { get; set; }
        
        [JsonPropertyName("velocidade")]
        public Vector2 Velocidade { get; set; }
        
        [JsonPropertyName("jogadorId")]
        public int JogadorId { get; set; }
        
        [JsonPropertyName("raio")]
        public float Raio { get; set; } = 3f;
        
        // Propriedades apenas do servidor
        [JsonIgnore]
        public DateTime CriadoEm { get; set; } = DateTime.Now;
        
        [JsonIgnore]
        public Vector2 PosicaoAnterior { get; set; }
        
        [JsonIgnore]
        public float DanoBase { get; set; } = 1f;
        
        [JsonIgnore]
        public bool JaColidiu { get; set; } = false;

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public TiroServidor() { }

        /// <summary>
        /// Construtor com parâmetros
        /// </summary>
        public TiroServidor(int id, Vector2 posicao, Vector2 velocidade, int jogadorId)
        {
            Id = id;
            Posicao = posicao;
            PosicaoAnterior = posicao;
            Velocidade = velocidade;
            JogadorId = jogadorId;
            CriadoEm = DateTime.Now;
            
            // Normalizar velocidade se necessário
            if (velocidade.Length() == 0)
            {
                Velocidade = new Vector2(0, -Constantes.VELOCIDADE_TIRO);
            }
            else if (velocidade.Length() != Constantes.VELOCIDADE_TIRO)
            {
                Velocidade = Vector2.Normalize(velocidade) * Constantes.VELOCIDADE_TIRO;
            }
        }

        /// <summary>
        /// Atualiza posição do tiro
        /// </summary>
        public void Atualizar(float deltaTime)
        {
            PosicaoAnterior = Posicao;
            Posicao += Velocidade * deltaTime * 60f; // Normalizar para 60 FPS
        }

        /// <summary>
        /// Verifica se o tiro saiu da área de jogo
        /// </summary>
        public bool EstaForaDaArea()
        {
            const int margem = 20;
            return Posicao.X < -margem || 
                   Posicao.X > Constantes.LARGURA_TELA + margem ||
                   Posicao.Y < -margem || 
                   Posicao.Y > Constantes.ALTURA_TELA + margem;
        }

        /// <summary>
        /// Verifica se o tiro é muito antigo (evitar tiros "eternos")
        /// </summary>
        public bool EhMuitoAntigo()
        {
            return TempoVida.TotalSeconds > 8; // 8 segundos máximo
        }

        /// <summary>
        /// Verifica colisão com outro objeto circular
        /// </summary>
        public bool ColidiuCom(Vector2 posicaoOutro, float raioOutro)
        {
            if (JaColidiu) return false; // Tiro já colidiu
            
            var distanciaQuadrada = Vector2.DistanceSquared(Posicao, posicaoOutro);
            var raioSomaQuadrada = (Raio + raioOutro) * (Raio + raioOutro);
            return distanciaQuadrada <= raioSomaQuadrada;
        }

        /// <summary>
        /// Marca o tiro como tendo colidido
        /// </summary>
        public void MarcarColisao()
        {
            JaColidiu = true;
        }

        /// <summary>
        /// Tempo de vida do tiro
        /// </summary>
        public TimeSpan TempoVida => DateTime.Now - CriadoEm;

        /// <summary>
        /// Velocidade atual do tiro
        /// </summary>
        public float VelocidadeAtual => Velocidade.Length();

        /// <summary>
        /// Direção normalizada do tiro
        /// </summary>
        public Vector2 Direcao => Velocidade.Length() > 0 ? Vector2.Normalize(Velocidade) : Vector2.Zero;

        /// <summary>
        /// Distância percorrida desde a criação
        /// </summary>
        public float DistanciaPercorrida => Vector2.Distance(Posicao, PosicaoAnterior);

        /// <summary>
        /// Verifica se é um tiro válido
        /// </summary>
        public bool EhValido => !JaColidiu && Velocidade.Length() > 0.1f;

        /// <summary>
        /// Calcula posição futura do tiro
        /// </summary>
        public Vector2 PosicaoFutura(float tempoSegundos)
        {
            return Posicao + Velocidade * tempoSegundos * 60f;
        }

        /// <summary>
        /// Verifica se vai interceptar um alvo em movimento
        /// </summary>
        public bool VaiInterceptar(Vector2 posicaoAlvo, Vector2 velocidadeAlvo, float raioAlvo, float tempoMaximo)
        {
            for (float t = 0; t <= tempoMaximo; t += 0.1f)
            {
                var posicaoTiroFutura = PosicaoFutura(t);
                var posicaoAlvoFutura = posicaoAlvo + velocidadeAlvo * t * 60f;
                
                var distancia = Vector2.Distance(posicaoTiroFutura, posicaoAlvoFutura);
                if (distancia <= Raio + raioAlvo)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Aplica modificador de dano baseado na distância percorrida
        /// </summary>
        public float DanoAtual
        {
            get
            {
                var distanciaTotal = Vector2.Distance(Posicao, PosicaoAnterior);
                // Dano diminui com a distância (simulando perda de energia)
                var fatorDistancia = Math.Max(0.5f, 1f - distanciaTotal / 1000f);
                return DanoBase * fatorDistancia;
            }
        }

        /// <summary>
        /// Converte para modelo de rede
        /// </summary>
        public TiroRede ParaRede()
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
        /// Cria trajetória do tiro para previsão
        /// </summary>
        public List<Vector2> CriarTrajetoria(float tempoMaximo, float intervaloPasso = 0.1f)
        {
            var trajetoria = new List<Vector2>();
            var posicaoAtual = Posicao;
            
            for (float t = 0; t <= tempoMaximo; t += intervaloPasso)
            {
                posicaoAtual += Velocidade * intervaloPasso * 60f;
                trajetoria.Add(posicaoAtual);
                
                // Parar se sair da tela
                if (posicaoAtual.X < -50 || posicaoAtual.X > Constantes.LARGURA_TELA + 50 ||
                    posicaoAtual.Y < -50 || posicaoAtual.Y > Constantes.ALTURA_TELA + 50)
                {
                    break;
                }
            }
            
            return trajetoria;
        }

        /// <summary>
        /// Representação string para debug
        /// </summary>
        public override string ToString()
        {
            var status = JaColidiu ? "COLIDIU" : "ATIVO";
            return $"Tiro[{Id}] do Jogador {JogadorId} em ({Posicao.X:F1}, {Posicao.Y:F1}) " +
                   $"vel({Velocidade.X:F1}, {Velocidade.Y:F1}) {status} " +
                   $"idade={TempoVida.TotalSeconds:F1}s";
        }

        /// <summary>
        /// Cria cópia do tiro
        /// </summary>
        public TiroServidor Clone()
        {
            return new TiroServidor
            {
                Id = Id,
                Posicao = Posicao,
                PosicaoAnterior = PosicaoAnterior,
                Velocidade = Velocidade,
                JogadorId = JogadorId,
                Raio = Raio,
                DanoBase = DanoBase,
                JaColidiu = JaColidiu,
                CriadoEm = CriadoEm
            };
        }

        /// <summary>
        /// Verifica igualdade baseada no ID
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is TiroServidor other && Id == other.Id;
        }

        /// <summary>
        /// Hash code baseado no ID
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Cria tiro padrão (para cima)
        /// </summary>
        public static TiroServidor CriarTiroNormal(int id, Vector2 posicaoInicial, int jogadorId)
        {
            var velocidade = new Vector2(0, -Constantes.VELOCIDADE_TIRO);
            return new TiroServidor(id, posicaoInicial, velocidade, jogadorId);
        }

        /// <summary>
        /// Cria tiro direcionado para um alvo
        /// </summary>
        public static TiroServidor CriarTiroDirecionado(int id, Vector2 posicaoInicial, Vector2 alvo, int jogadorId)
        {
            var direcao = Vector2.Normalize(alvo - posicaoInicial);
            var velocidade = direcao * Constantes.VELOCIDADE_TIRO;
            return new TiroServidor(id, posicaoInicial, velocidade, jogadorId);
        }
    }
}