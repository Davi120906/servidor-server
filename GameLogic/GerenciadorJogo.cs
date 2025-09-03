using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using AsteroidesServidor.Modelos;
using AsteroidesServidor.Rede;
using AsteroidesServidor.Utils;

namespace AsteroidesServidor.GameLogic
{
    /// <summary>
    /// Gerenciador principal da lógica do jogo
    /// Implementa paralelismo para otimização (REQUISITO)
    /// </summary>
    public class GerenciadorJogo
    {
        // Coleções thread-safe para suporte a paralelismo
        private readonly ConcurrentDictionary<int, EstadoJogador> _jogadores = new();
        private readonly ConcurrentDictionary<int, AsteroideServidor> _asteroides = new();
        private readonly ConcurrentDictionary<int, TiroServidor> _tiros = new();

        private readonly GerenciadorColisoes _colisoes = new();
        private readonly Random _random = new();
        private readonly object _jogoLock = new();

        // Contadores e controle
        private int _proximoIdAsteroide = 1;
        private int _proximoIdTiro = 1;
        private long _frameCount = 0;
        private int _framesDesdeUltimoAsteroide = 0;
        private bool _jogoAtivo = true;

        // Eventos
       public event Action<EstadoJogo>? EstadoJogoAtualizado;


        public event Func<int, MensagemRede, Task>? MensagemParaJogador;


        // Propriedades para monitoramento
        public int NumeroJogadores => _jogadores.Count;
        public int NumeroAsteroides => _asteroides.Count;
        public int NumeroTiros => _tiros.Count;

        /// <summary>
        /// Adiciona novo jogador ao jogo
        /// </summary>
        public void AdicionarJogador(ClienteConectado cliente)
        {
            var jogador = new EstadoJogador
            {
                Id = cliente.Id,
                Nome = $"Jogador {cliente.Id}",
                Posicao = new Vector2(
                    Constantes.LARGURA_TELA / 2f,
                    Constantes.ALTURA_TELA - 60
                ),
                Ativo = true,
                Vidas = 3,
                Pontuacao = 0
            };

            _jogadores[cliente.Id] = jogador;

            Console.WriteLine($"[GAME] Jogador {cliente.Id} adicionado ao jogo");

            // Enviar mensagem de boas-vindas
            MensagemParaJogador?.Invoke(cliente.Id, new MensagemRede
            {
                Tipo = "BEM_VINDO",
                Dados = $"Bem-vindo ao jogo, {jogador.Nome}!",
                JogadorId = cliente.Id
            });
        }

        /// <summary>
        /// Remove jogador do jogo
        /// </summary>
        public void RemoverJogador(int jogadorId)
        {
            if (_jogadores.TryRemove(jogadorId, out var jogador))
            {
                Console.WriteLine($"[GAME] Jogador {jogadorId} removido do jogo");

                // Remover tiros do jogador
                var tirosDoJogador = _tiros.Where(t => t.Value.JogadorId == jogadorId).ToList();
                foreach (var tiro in tirosDoJogador)
                {
                    _tiros.TryRemove(tiro.Key, out _);
                }
            }
        }

        /// <summary>
        /// Processa mensagens recebidas dos jogadores
        /// </summary>
        public void ProcessarMensagemJogador(int jogadorId, MensagemRede mensagem)
        {
            if (!_jogadores.TryGetValue(jogadorId, out var jogador) || !jogador.Ativo)
                return;

            try
            {
                switch (mensagem.Tipo)
                {
                    case Constantes.TipoMensagem.MOVIMENTO:
                        ProcessarMovimento(jogadorId, mensagem.Dados);
                        break;

                    case Constantes.TipoMensagem.TIRO:
                        ProcessarTiro(jogadorId, mensagem.Dados);
                        break;

                    case Constantes.TipoMensagem.PING:
                        // Responder com pong
                        MensagemParaJogador?.Invoke(jogadorId, new MensagemRede
                        {
                            Tipo = Constantes.TipoMensagem.PONG,
                            Dados = mensagem.Dados,
                            JogadorId = jogadorId
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GAME] Erro ao processar mensagem do jogador {jogadorId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loop principal de atualização do jogo
        /// </summary>
        public void AtualizarJogo(TimeSpan deltaTime)
        {
            if (!_jogoAtivo || _jogadores.IsEmpty) return;

            lock (_jogoLock)
            {
                _frameCount++;

                // 1. Atualizar objetos em paralelo (REQUISITO: otimização paralela)
                AtualizarObjetosParalelo(deltaTime);

                // 2. Verificar colisões em paralelo (REQUISITO: tarefa computacionalmente pesada)
                VerificarColisoesParalelo();

                // 3. Remover objetos fora da tela
                RemoverObjetosForaDaTela();

                // 4. Gerar novos asteroides
                GerarAsteroides();

                // 5. Verificar game over
                VerificarGameOver();

                // 6. Enviar estado atualizado para clientes
                EnviarEstadoJogo();
            }
        }

        /// <summary>
        /// Atualiza posições de todos os objetos usando paralelismo (REQUISITO)
        /// </summary>
        private void AtualizarObjetosParalelo(TimeSpan deltaTime)
        {
            var dt = (float)deltaTime.TotalSeconds;

            // Atualizar asteroides em paralelo (REQUISITO: Parallel.ForEach)
            Parallel.ForEach(_asteroides.Values, asteroide =>
            {
                asteroide.Atualizar(dt);

                // Wrap around nas bordas
                if (asteroide.Posicao.X < -50)
                    asteroide.Posicao = new Vector2(Constantes.LARGURA_TELA + 50, asteroide.Posicao.Y);
                else if (asteroide.Posicao.X > Constantes.LARGURA_TELA + 50)
                    asteroide.Posicao = new Vector2(-50, asteroide.Posicao.Y);
            });

            // Atualizar tiros em paralelo (REQUISITO: Parallel.ForEach)
            Parallel.ForEach(_tiros.Values, tiro =>
            {
                tiro.Atualizar(dt);
            });
        }

        /// <summary>
        /// Verifica colisões usando programação paralela (REQUISITO: tarefa computacionalmente pesada)
        /// JUSTIFICATIVA: Verificação de colisões é O(n²) e se beneficia do paralelismo
        /// especialmente com muitos asteroides e tiros simultâneos
        /// </summary>
        private void VerificarColisoesParalelo()
        {
            // Converter para listas para evitar modificações durante iteração
            var asteroidesLista = _asteroides.Values.ToList();
            var tirosLista = _tiros.Values.ToList();
            var jogadoresLista = _jogadores.Values.Where(j => j.Ativo).ToList();

            // Collections thread-safe para resultados
            var asteroidesPraRemover = new ConcurrentBag<int>();
            var tirosPraRemover = new ConcurrentBag<int>();
            var jogadoresAtingidos = new ConcurrentBag<int>();

            // 1. Colisões Tiro-Asteroide em paralelo (REQUISITO: PLINQ)
            asteroidesLista.AsParallel().ForAll(asteroide =>
            {
                foreach (var tiro in tirosLista)
                {
                    if (_colisoes.VerificarColisao(tiro.Posicao, tiro.Raio, asteroide.Posicao, asteroide.Raio))
                    {
                        asteroidesPraRemover.Add(asteroide.Id);
                        tirosPraRemover.Add(tiro.Id);

                        // Adicionar pontuação ao jogador
                        if (_jogadores.TryGetValue(tiro.JogadorId, out var jogador))
                        {
                            jogador.Pontuacao += CalcularPontuacaoAsteroide(asteroide.Raio);
                        }

                        break; // Asteroide já foi atingido
                    }
                }
            });

            // 2. Colisões Jogador-Asteroide em paralelo (REQUISITO: PLINQ)
            jogadoresLista.AsParallel().ForAll(jogador =>
            {
                foreach (var asteroide in asteroidesLista)
                {
                    if (_colisoes.VerificarColisao(jogador.Posicao, 12f, asteroide.Posicao, asteroide.Raio))
                    {
                        jogadoresAtingidos.Add(jogador.Id);
                        break; // Jogador já foi atingido
                    }
                }
            });

            // Aplicar resultados das colisões
            AplicarResultadosColisoes(asteroidesPraRemover, tirosPraRemover, jogadoresAtingidos, asteroidesLista);
        }

        /// <summary>
        /// Aplica os resultados das verificações de colisão
        /// </summary>
        private void AplicarResultadosColisoes(
            ConcurrentBag<int> asteroidesPraRemover,
            ConcurrentBag<int> tirosPraRemover,
            ConcurrentBag<int> jogadoresAtingidos,
            List<AsteroideServidor> asteroidesOriginais)
        {
            // Remover asteroides atingidos
            foreach (var asteroidId in asteroidesPraRemover.Distinct())
            {
                if (_asteroides.TryRemove(asteroidId, out var asteroide))
                {
                    // Fragmentar asteroide grande
                    if (asteroide.Raio > 20)
                    {
                        CriarFragmentosAsteroide(asteroide);
                    }
                }
            }

            // Remover tiros que acertaram
            foreach (var tiroId in tirosPraRemover.Distinct())
            {
                _tiros.TryRemove(tiroId, out _);
            }

            // Processar jogadores atingidos
            foreach (var jogadorId in jogadoresAtingidos.Distinct())
            {
                if (_jogadores.TryGetValue(jogadorId, out var jogador))
                {
                    jogador.Vidas--;

                    if (jogador.Vidas <= 0)
                    {
                        jogador.Ativo = false;
                        Console.WriteLine($"[GAME] Jogador {jogadorId} foi eliminado!");
                    }
                    else
                    {
                        // Reposicionar jogador (respawn)
                        jogador.Posicao = new Vector2(
                            Constantes.LARGURA_TELA / 2f,
                            Constantes.ALTURA_TELA - 60
                        );
                        Console.WriteLine($"[GAME] Jogador {jogadorId} perdeu uma vida. Vidas restantes: {jogador.Vidas}");
                    }
                }
            }
        }

        /// <summary>
        /// Cria fragmentos menores quando asteroide grande é destruído
        /// </summary>
        private void CriarFragmentosAsteroide(AsteroideServidor asteroideOriginal)
        {
            var novoRaio = asteroideOriginal.Raio * 0.6f;

            if (novoRaio < 15) return; // Muito pequeno para fragmentar

            // Criar 2-3 fragmentos
            var numFragmentos = _random.Next(2, 4);

            for (int i = 0; i < numFragmentos; i++)
            {
                var angulo = (float)(2 * Math.PI * i / numFragmentos);
                var deslocamento = new Vector2(
                    (float)Math.Cos(angulo) * 30,
                    (float)Math.Sin(angulo) * 30
                );

                var velocidadeFragmento = new Vector2(
                    asteroideOriginal.Velocidade.X + (float)(_random.NextDouble() - 0.5) * 4,
                    asteroideOriginal.Velocidade.Y + (float)(_random.NextDouble() - 0.5) * 4
                );

                var fragmento = new AsteroideServidor
                {
                    Id = Interlocked.Increment(ref _proximoIdAsteroide),
                    Posicao = asteroideOriginal.Posicao + deslocamento,
                    Velocidade = velocidadeFragmento,
                    Raio = novoRaio
                };

                _asteroides[fragmento.Id] = fragmento;
            }
        }

        /// <summary>
        /// Remove objetos que saíram da tela
        /// </summary>
        private void RemoverObjetosForaDaTela()
        {
            // Remover tiros fora da tela
            var tirosForaDaTela = _tiros.Where(t =>
                t.Value.Posicao.Y < -10 ||
                t.Value.Posicao.Y > Constantes.ALTURA_TELA + 10 ||
                t.Value.Posicao.X < -10 ||
                t.Value.Posicao.X > Constantes.LARGURA_TELA + 10
            ).ToList();

            foreach (var tiro in tirosForaDaTela)
            {
                _tiros.TryRemove(tiro.Key, out _);
            }

            // Remover asteroides que saíram muito para baixo
            var asteroidesForaDaTela = _asteroides.Where(a =>
                a.Value.Posicao.Y > Constantes.ALTURA_TELA + 100
            ).ToList();

            foreach (var asteroide in asteroidesForaDaTela)
            {
                _asteroides.TryRemove(asteroide.Key, out _);
            }
        }

        /// <summary>
        /// Gera novos asteroides periodicamente
        /// </summary>
        private void GerarAsteroides()
        {
            _framesDesdeUltimoAsteroide++;

            if (_framesDesdeUltimoAsteroide >= Constantes.SPAWN_ASTEROIDE_FRAMES &&
                _asteroides.Count < Constantes.MAX_ASTEROIDES)
            {
                CriarNovoAsteroide();
                _framesDesdeUltimoAsteroide = 0;
            }
        }

        /// <summary>
        /// Cria um novo asteroide em posição aleatória
        /// </summary>
        private void CriarNovoAsteroide()
        {
            var posicaoX = _random.Next(-50, Constantes.LARGURA_TELA + 50);
            var posicaoY = -50;

            var velocidadeX = (float)(_random.NextDouble() - 0.5) * 4; // -2 a +2
            var velocidadeY = (float)(_random.NextDouble() * 2 + 1);   // 1 a 3

            var raio = (float)(_random.NextDouble() * 30 + 20); // 20 a 50

            var asteroide = new AsteroideServidor
            {
                Id = Interlocked.Increment(ref _proximoIdAsteroide),
                Posicao = new Vector2(posicaoX, posicaoY),
                Velocidade = new Vector2(velocidadeX, velocidadeY),
                Raio = raio
            };

            _asteroides[asteroide.Id] = asteroide;
        }

        /// <summary>
        /// Verifica condições de game over
        /// </summary>
        private void VerificarGameOver()
        {
            var jogadoresAtivos = _jogadores.Values.Count(j => j.Ativo);

            if (jogadoresAtivos == 0 && _jogadores.Count > 0)
            {
                Console.WriteLine("[GAME] Game Over - Todos os jogadores foram eliminados!");
                // Manter o jogo ativo para permitir reconexões
            }
        }

        /// <summary>
        /// Envia estado atual do jogo para todos os clientes
        /// </summary>
        private void EnviarEstadoJogo()
        {
            var estado = new EstadoJogo
            {
                Jogadores = _jogadores.Values.ToList(),
                Asteroides = _asteroides.Values.Select(a => new AsteroideRede
                {
                    Id = a.Id,
                    Posicao = a.Posicao,
                    Velocidade = a.Velocidade,
                    Raio = a.Raio
                }).ToList(),
                Tiros = _tiros.Values.Select(t => new TiroRede
                {
                    Id = t.Id,
                    Posicao = t.Posicao,
                    Velocidade = t.Velocidade,
                    JogadorId = t.JogadorId
                }).ToList(),
                FrameCount = _frameCount,
                JogoAtivo = _jogoAtivo
            };

            EstadoJogoAtualizado?.Invoke(estado);
        }

        /// <summary>
        /// Processa movimento do jogador
        /// </summary>
        private void ProcessarMovimento(int jogadorId, string dados)
        {
            try
            {
                var novaPosicao = JsonSerializer.Deserialize<Vector2>(dados);

                if (_jogadores.TryGetValue(jogadorId, out var jogador))
                {
                    // Validar posição (anti-cheat básico)
                    novaPosicao.X = Math.Clamp(novaPosicao.X, 10, Constantes.LARGURA_TELA - 10);
                    novaPosicao.Y = Math.Clamp(novaPosicao.Y, 10, Constantes.ALTURA_TELA - 10);

                    jogador.Posicao = novaPosicao;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GAME] Erro ao processar movimento do jogador {jogadorId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa tiro disparado pelo jogador
        /// </summary>
        private void ProcessarTiro(int jogadorId, string dados)
        {
            try
            {
                var tiroData = JsonSerializer.Deserialize<TiroRede>(dados);

                if (tiroData != null && _jogadores.ContainsKey(jogadorId))
                {
                    var tiro = new TiroServidor
                    {
                        Id = Interlocked.Increment(ref _proximoIdTiro),
                        Posicao = tiroData.Posicao,
                        Velocidade = tiroData.Velocidade,
                        JogadorId = jogadorId,
                        Raio = 2f
                    };

                    _tiros[tiro.Id] = tiro;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GAME] Erro ao processar tiro do jogador {jogadorId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcula pontuação baseada no tamanho do asteroide
        /// </summary>
        private int CalcularPontuacaoAsteroide(float raio)
        {
            if (raio > 40) return 20;      // Asteroide grande
            if (raio > 25) return 50;      // Asteroide médio  
            return 100;                    // Asteroide pequeno
        }

        /// <summary>
        /// Finaliza o gerenciador de jogo
        /// </summary>
        public void Finalizar()
        {
            _jogoAtivo = false;
            _jogadores.Clear();
            _asteroides.Clear();
            _tiros.Clear();
            Console.WriteLine("[GAME] Gerenciador de jogo finalizado");
        }
    }
}