namespace AsteroidesServidor.Utils
{
    public static class Constantes
    {
        // Tamanho da tela do jogo
        public const int LARGURA_TELA = 800;
        public const int ALTURA_TELA = 600;

        // Definição de velocidade dos tiros
        public const float VELOCIDADE_TIRO = 10f;

        // Tempo de intervalo entre os frames (60 FPS)
        public const double INTERVALO_FRAME = 1000.0 / 60.0; // Em milissegundos

        // Porta padrão para o servidor TCP
        public const int PORTA_PADRAO = 12345;

        // Tipos de mensagens para rede
        public static class TipoMensagem
        {
            public const string ID_JOGADOR = "ID_JOGADOR";
            public const string ESTADO_JOGO = "ESTADO_JOGO";
            public const string PING = "PING";
            public const string MOVIMENTO = "MOVIMENTO";   // Nova constante
            public const string TIRO = "TIRO";             // Nova constante
            public const string PONG = "PONG";  
        }
        
        public const int PONTOS_ASTEROIDE_GRANDE = 100;
        public const int PONTOS_ASTEROIDE_MEDIO = 50;
        public const int PONTOS_ASTEROIDE_PEQUENO = 25;

        public const float RAIO_MIN_ASTEROIDE = 10f;  
        public const float RAIO_MAX_ASTEROIDE = 30f;
        public const int SPAWN_ASTEROIDE_FRAMES = 60;
        public const int MAX_ASTEROIDES = 50;
        public const int NUMERO_MAXIMO_JOGADORES = 4;  
        public const int NUMERO_MAXIMO_ASTEROIDES = 50; // Número máximo de asteroides no jogo
        public const int NUMERO_MAXIMO_TIROS = 100;     // Limite de tiros simultâneos

        // Raio dos objetos do jogo
        public const float RAIO_JOGADOR = 15f;         // Raio do jogador
        public const float RAIO_ASTEROIDE = 20f;       // Raio de um asteroide
        public const float RAIO_TIRO = 3f;             // Raio do tiro

        // Tempo máximo de vida de um tiro (8 segundos)
        public const int TEMPO_MAXIMO_TIRO = 8; // em segundos

        // Configuração de colisões (margem)
        public const int MARGEM_COLISAO = 20;

        // Limite de distância máxima para considerar colisão
        public const float DISTANCIA_MAXIMA_COLISAO = 1000f;

        // Frequência de atualização de estado do jogo (em milissegundos)
        public const int INTERVALO_ATUALIZACAO_ESTADO = 100; // 100ms = 10 updates por segundo

        // Configurações de Debug
        public const bool DEBUG = false;  // Controla o nível de debug (habilitar/disabled logs extra)
    }
}
