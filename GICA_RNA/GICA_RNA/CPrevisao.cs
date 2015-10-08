using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

using Accord;
using Accord.Neuro;
using Accord.Neuro.Learning;

using AForge;
using AForge.Neuro;
using AForge.Neuro.Learning;

namespace GICA_RNA
{
    public class CPrevisao
    {
        #region Atributos privados

        //Variáveis de redes
        //private ActivationNetwork network; //rede neural propiamente dita
        //private ActivationNetwork networkValidation; //rede neural aprovada pela validação

        //Variáveis auxiliares para rede
        private double fatorNormal; //fator de conversão para normalização dos dados

        //Variáveis de controle
        private Thread workerThread = null; //thread do aprendizado
        private Thread validationThread = null; //thread utilizada na validação
        private volatile bool needToStop = false; //variável auxiliar que define quando o processo de treino acaba

        //Variáveis dos resultados relativos a previsão
        private double[,] resultTreino;
        private double[,] resultValidation;
        private double[,] resultTeste;

        //Dados
        private List<double> data = new List<double>();

        //Instância do objeto RedeNeural
        private CRedeNeural RNA =  new CRedeNeural();
        private CSerieTemporal Serie;

        #endregion

        #region Atributos públicos

        public double learningMAPE = 0.0;
        public double validationMAPE = 0.0;
        public double predictionMAPE = 0.0;

        public double learningUtheil = 0.0;
        public double validationUtheil = 0.0;
        public double predictionUtheil = 0.0;

        public double learningError = 0.0;
        public double validationError = 0.0;
        public double predictionError = 0.0;

        #endregion

        #region Metodos Publicos

        /// <summary>
        /// Metodo que retorna uma lista dos dados previstos. ****
        /// </summary>
        /// <param name="dados">Dados reais a serem trabalhados.</param>
        /// <param name="xInicial">Valor do indice inicial dos dados começando de 1. Default: 1.</param>
        /// <returns></returns>
        public List<double> Previsao(List<double> dados, int xInicial = 1)
        {            
            //RNA.Algoritmo = "ParallelResilientBackpropagationLearning";            

            //Instancia a classe CSerieTemporal.
            Serie = new CSerieTemporal(dados, RNA, xInicial);

            //Aplica a diferença nos dados e separa em treino, validação e teste.
            Serie.PrepararDados(true);

            TestarConfiguracoes();

            resultTreino = Treino();

            return null;
        }

        #endregion

        #region Metodos Privados

        private double[,] Treino()
        {
            //variável que conta a quantidade de iterações
            int iteration;

            //fator de normalização
            fatorNormal = 2.0/ (Serie.Max - Serie.Min);

            //lista contendo todos os ids de 1 a 52
            List<int> ids = Serie.Ids;
            //variavel que possuirá o id binário
            int[] id = new int[6];

            // número de amostras para a aprendizagem
            int samples = RNA.DadosTreino.Length/2 - RNA.WindowSize - RNA.PredictionSize;

            // preparação dos dados para a aprendizagem
            double[][] input = new double[samples][];//vetor de entrada
            double[][] output = new double[samples][];//vetor de saída

            for (int i = 0; i < samples; i++)
            {
                //define o tamanho da entrada 
                input[i] = new double[RNA.WindowSize + RNA.PredictionSize * 6];
                //define o tamanho da saída
                output[i] = new double[RNA.PredictionSize];

                // configura a entrada com os dados formatados
                for (int j = 0; j < RNA.WindowSize + RNA.PredictionSize * 6; j++)
                {
                    if (j < RNA.WindowSize)
                    {
                        input[i][j] = (RNA.DadosTreino[i + j, 1] - Serie.Min) * fatorNormal - 1.0;
                    }
                    else
                    {
                        if (j == RNA.WindowSize)
                            id = CUtil.ConversaoBinario(ids[i + RNA.WindowSize]);

                        input[i][j] = id[j - RNA.WindowSize];
                    }
                }// fim do for interno

                // configura os dados de saída com os dados transformados
                for (int k = 0; k < RNA.PredictionSize; k++)
                {
                    output[i][k] = (RNA.DadosTreino[i + k + RNA.WindowSize, 1] - Serie.Min) * fatorNormal - 1;
                }
            }//fim do for externo

            //cria o "Professor" da rede neural
            ParallelResilientBackpropagationLearning teacher = new ParallelResilientBackpropagationLearning(RNA.Network);

            //Variável para contar o número de iterações
            iteration = 1;

            //vetor que armazena a solução encontrada pela rede neural
            int solutionSize = RNA.DadosTreino.Length/2 - RNA.WindowSize;
            double[,] solution = new double[solutionSize, 2];

            //Vetor auxiliar que seta as entrada a serem computadas pela rede neural
            double[] networkInput = new double[RNA.WindowSize + RNA.PredictionSize * 6];

            //calcula os valores do eixo X a serem utilizados
            for (int j = 0; j < (solutionSize); j++)
            {
                //os pontos são retirados do vetor que contém os pontos x associados as entradas
                solution[j, 0] = Serie.Dados[j + RNA.WindowSize, 0] + 1;
            }//fim do for

            // loop que efetua as iterações
            while (!needToStop)
            {
                learningError = 0.0;

                //roda uma iteração do processo de aprendizagem retornando o erro obtido
                double error = teacher.RunEpoch(input, output) / samples;

                //variaveis auxiliares para calculo do utheil
                double somaY = 0.0;
                double somaF = 0.0;

                //variavel auxiliar para calcular os erros
                int amostra = 0;

                //variavel auxiliar para o id binário
                int contador = 0;

                // computa as saídas através de toda a lista de dados, armazena os valores de saída da rede em solution
                for (int i = 0, n = RNA.DadosTreino.Length/2 - RNA.WindowSize - RNA.PredictionSize + 1; i < n; i++)
                {
                    int a = RNA.WindowSize;
                    contador = 0;
                    // seta os valores da atual janela de previsão como entrada da rede neural
                    for (int j = 0; j < RNA.WindowSize + RNA.PredictionSize; j++)
                    {
                        if (j < RNA.WindowSize)
                        {
                            //entrada tem de ser formatada
                            networkInput[j] = (RNA.DadosTreino[i + j, 1] - Serie.Min) * fatorNormal - 1.0;
                        }
                        else
                        {
                            id = CUtil.ConversaoBinario(ids[i + a]);
                            a++;

                            for (int c = 0; c < 6; c++)
                            {                                
                                networkInput[RNA.WindowSize + contador] = id[c];
                                contador++;
                            }
                        }//fim do else
                    }//fim do for interno                          

                    //computa a saída da rede e armazena o dado solution diferenca
                    for (int k = 0; k < RNA.Network.Compute(networkInput).Length; k++)
                    {
                        double diferenca = (RNA.Network.Compute(networkInput)[k] + 1.0) / fatorNormal + Serie.Min;
                        if ((i + k) < solutionSize) solution[i + k, 1] = (diferenca) + Serie.Dados[RNA.WindowSize + i, 1];
                    }

                    //calcula o erro de aprendizagem
                    amostra++;
                    
                    //variaveis auxiliares do u theil
                    somaY += ((Serie.Dados[RNA.WindowSize + i, 1]) * (Serie.Dados[RNA.WindowSize + i, 1]));
                    somaF += ((solution[i, 1] * solution[i, 1]));

                    learningError += ((solution[i, 1] - Serie.Dados[RNA.WindowSize + i + 1, 1]) * (solution[i, 1] - Serie.Dados[RNA.WindowSize + i, 1]));

                }//fim do for externo

                learningError = (learningError) / amostra;
                somaF = somaF / amostra;
                somaY = somaY / amostra;

                learningUtheil = Math.Sqrt(learningError) / (Math.Sqrt(somaY) + Math.Sqrt(somaF));

                // incrementa a iteração atual
                iteration++;

                // confere se precisamos ou não parar, fator de parada é o número de iterações
                if (iteration > 400)
                    break;
            }//fim do while

            return solution;
        }

        private void TestarConfiguracoes()
        {
            // Limpa a rede neural anterior e reutiliza 
            RNA.Network = null;

            List<int> neurons = new List<int>();
            neurons.Add(25);
            neurons.Add(15);
            RNA.Neuronios = neurons;
            RNA.PredictionSize = 1;
            RNA.WindowSize = 5;

            RNA.configuraRede();
        }

        #endregion
    }
}
