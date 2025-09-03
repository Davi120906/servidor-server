using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace AsteroidesServidor.GameLogic
{
    
    public class GerenciadorColisoes
    {
        private readonly bool _simdDisponivel;

        public GerenciadorColisoes()
        {
            _simdDisponivel = Vector.IsHardwareAccelerated && Sse.IsSupported;
            
            if (_simdDisponivel)
            {
                Console.WriteLine("[COLISOES] Otimizações SIMD habilitadas");
            }
            else
            {
                Console.WriteLine("[COLISOES] Usando cálculos escalares tradicionais");
            }
        }

        /// <summary>
        /// Verifica colisão entre dois objetos circulares
        /// </summary>
        public bool VerificarColisao(Vector2 pos1, float raio1, Vector2 pos2, float raio2)
        {
            var distanciaQuadrada = DistanciaQuadrada(pos1, pos2);
            var raioSomaQuadrada = (raio1 + raio2) * (raio1 + raio2);
            
            return distanciaQuadrada <= raioSomaQuadrada;
        }

        /// <summary>
        /// Calcula distância quadrada entre dois pontos
        /// Evita sqrt() para melhor performance
        /// </summary>
        public float DistanciaQuadrada(Vector2 a, Vector2 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Verifica múltiplas colisões usando SIMD quando possível (REQUISITO: pontos extras)
        /// </summary>
        public unsafe bool[] VerificarColisoesLote(
            Vector2[] posicoes1, float[] raios1,
            Vector2[] posicoes2, float[] raios2)
        {
            var count = Math.Min(posicoes1.Length, posicoes2.Length);
            var resultados = new bool[count];

            if (_simdDisponivel && count >= 4)
            {
                VerificarColisoesLoteSIMD(posicoes1, raios1, posicoes2, raios2, resultados);
            }
            else
            {
                VerificarColisoesLoteEscalar(posicoes1, raios1, posicoes2, raios2, resultados);
            }

            return resultados;
        }

        /// <summary>
        /// Implementação SIMD para verificação de colisões em lote (REQUISITO: pontos extras)
        /// JUSTIFICATIVA: Processamento vetorial permite calcular 4 colisões simultaneamente
        /// </summary>
        private unsafe void VerificarColisoesLoteSIMD(
            Vector2[] pos1, float[] raios1,
            Vector2[] pos2, float[] raios2,
            bool[] resultados)
        {
            int i = 0;
            int count = resultados.Length;
            
            // Processar em grupos de 4 usando SIMD
            for (; i <= count - 4; i += 4)
            {
                // Carregar posições X e Y
                var x1 = Vector128.Create(pos1[i].X, pos1[i+1].X, pos1[i+2].X, pos1[i+3].X);
                var y1 = Vector128.Create(pos1[i].Y, pos1[i+1].Y, pos1[i+2].Y, pos1[i+3].Y);
                var x2 = Vector128.Create(pos2[i].X, pos2[i+1].X, pos2[i+2].X, pos2[i+3].X);
                var y2 = Vector128.Create(pos2[i].Y, pos2[i+1].Y, pos2[i+2].Y, pos2[i+3].Y);
                
                // Calcular diferenças
                var dx = Sse.Subtract(x1, x2);
                var dy = Sse.Subtract(y1, y2);
                
                // Calcular distâncias quadradas
                var dxSquared = Sse.Multiply(dx, dx);
                var dySquared = Sse.Multiply(dy, dy);
                var distanciaQuadrada = Sse.Add(dxSquared, dySquared);
                
                // Calcular raios somados
                var r1 = Vector128.Create(raios1[i], raios1[i+1], raios1[i+2], raios1[i+3]);
                var r2 = Vector128.Create(raios2[i], raios2[i+1], raios2[i+2], raios2[i+3]);
                var raioSoma = Sse.Add(r1, r2);
                var raioSomaQuadrada = Sse.Multiply(raioSoma, raioSoma);
                
                // Comparar e extrair resultados
                var mask = Sse.CompareLessThanOrEqual(distanciaQuadrada, raioSomaQuadrada);
                
                // Extrair resultados booleanos
                var maskInt = Sse2.MoveMask(mask);
                
                resultados[i] = (maskInt & 0x01) != 0;
                resultados[i+1] = (maskInt & 0x10) != 0;
                resultados[i+2] = (maskInt & 0x100) != 0;
                resultados[i+3] = (maskInt & 0x1000) != 0;
            }
            
            // Processar elementos restantes
            for (; i < count; i++)
            {
                resultados[i] = VerificarColisao(pos1[i], raios1[i], pos2[i], raios2[i]);
            }
        }

        /// <summary>
        /// Implementação escalar tradicional para verificação de colisões em lote
        /// </summary>
        private void VerificarColisoesLoteEscalar(
            Vector2[] pos1, float[] raios1,
            Vector2[] pos2, float[] raios2,
            bool[] resultados)
        {
            for (int i = 0; i < resultados.Length; i++)
            {
                resultados[i] = VerificarColisao(pos1[i], raios1[i], pos2[i], raios2[i]);
            }
        }

        /// <summary>
        /// Verifica se um ponto está dentro de um retângulo
        /// </summary>
        public bool PontoNoRetangulo(Vector2 ponto, Vector2 retanguloMin, Vector2 retanguloMax)
        {
            return ponto.X >= retanguloMin.X && ponto.X <= retanguloMax.X &&
                   ponto.Y >= retanguloMin.Y && ponto.Y <= retanguloMax.Y;
        }

        /// <summary>
        /// Verifica colisão entre círculo e retângulo
        /// </summary>
        public bool ColisaoCirculoRetangulo(Vector2 centroCirculo, float raio, 
                                          Vector2 retanguloMin, Vector2 retanguloMax)
        {
            // Encontrar ponto mais próximo no retângulo
            var pontoMaisProximo = new Vector2(
                Math.Clamp(centroCirculo.X, retanguloMin.X, retanguloMax.X),
                Math.Clamp(centroCirculo.Y, retanguloMin.Y, retanguloMax.Y)
            );
            
            // Verificar se a distância é menor que o raio
            var distanciaQuadrada = DistanciaQuadrada(centroCirculo, pontoMaisProximo);
            return distanciaQuadrada <= raio * raio;
        }

        /// <summary>
        /// Particionamento espacial simples para otimizar verificações de colisão
        /// Divide o espaço em células para reduzir verificações desnecessárias
        /// </summary>
        public class ParticaoEspacial
        {
            private readonly int _larguraCelula;
            private readonly int _alturaCelula;
            private readonly int _numCelulasX;
            private readonly int _numCelulasY;
            private readonly Dictionary<int, List<int>> _celulas;

            public ParticaoEspacial(int larguraTotal, int alturaTotal, int tamanhoCelula)
            {
                _larguraCelula = tamanhoCelula;
                _alturaCelula = tamanhoCelula;
                _numCelulasX = (larguraTotal + tamanhoCelula - 1) / tamanhoCelula;
                _numCelulasY = (alturaTotal + tamanhoCelula - 1) / tamanhoCelula;
                _celulas = new Dictionary<int, List<int>>();
            }

            /// <summary>
            /// Adiciona objeto à partição espacial
            /// </summary>
            public void AdicionarObjeto(int objetoId, Vector2 posicao, float raio)
            {
                var celulasOcupadas = GetCelulasOcupadas(posicao, raio);
                
                foreach (var celulaId in celulasOcupadas)
                {
                    if (!_celulas.ContainsKey(celulaId))
                    {
                        _celulas[celulaId] = new List<int>();
                    }
                    _celulas[celulaId].Add(objetoId);
                }
            }

            /// <summary>
            /// Obtém objetos próximos que podem colidir
            /// </summary>
            public HashSet<int> GetObjetosProximos(Vector2 posicao, float raio)
            {
                var objetosProximos = new HashSet<int>();
                var celulasOcupadas = GetCelulasOcupadas(posicao, raio);
                
                foreach (var celulaId in celulasOcupadas)
                {
                    if (_celulas.TryGetValue(celulaId, out var objetos))
                    {
                        foreach (var objeto in objetos)
                        {
                            objetosProximos.Add(objeto);
                        }
                    }
                }
                
                return objetosProximos;
            }

            /// <summary>
            /// Limpa todas as células
            /// </summary>
            public void Limpar()
            {
                _celulas.Clear();
            }

            /// <summary>
            /// Calcula quais células um objeto ocupa
            /// </summary>
            private List<int> GetCelulasOcupadas(Vector2 posicao, float raio)
            {
                var celulas = new List<int>();
                
                var minX = Math.Max(0, (int)((posicao.X - raio) / _larguraCelula));
                var maxX = Math.Min(_numCelulasX - 1, (int)((posicao.X + raio) / _larguraCelula));
                var minY = Math.Max(0, (int)((posicao.Y - raio) / _alturaCelula));
                var maxY = Math.Min(_numCelulasY - 1, (int)((posicao.Y + raio) / _alturaCelula));
                
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        celulas.Add(y * _numCelulasX + x);
                    }
                }
                
                return celulas;
            }
        }

        /// <summary>
        /// Estatísticas de performance do sistema de colisões
        /// </summary>
        public class EstatisticasColisao
        {
            public long TotalVerificacoes { get; set; }
            public long ColisoesDetectadas { get; set; }
            public long TempoTotalMs { get; set; }
            public bool UsandoSIMD { get; set; }
            
            public double MediaVerificacoesPorMs => 
                TempoTotalMs > 0 ? (double)TotalVerificacoes / TempoTotalMs : 0;
                
            public double PercentualColisoes => 
                TotalVerificacoes > 0 ? (double)ColisoesDetectadas / TotalVerificacoes * 100 : 0;
        }
    }
}