using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AForge.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro;

namespace GICA_RNA
{
    class CRedeNeural
    {
        #region Atributos

        //Variáveis auxiliares para rede
        private double fatorNormal; //fator de conversão para normalização dos dados

        //Variáveis de dados
        private List<double> dadosTreino = new List<double>();
        private List<double> dadosValidacao = new List<double>();
        private List<double> dadosTeste = new List<double>();  

        //Variáveis dos resultados relativos a previsão
        private List<double> resultValidation = new List<double>();
        private List<double> resultTeste = new List<double>();
        private List<double> result = new List<double>();

        //solução temporaria
        private List<double> resultValidationTemp = new List<double>();

        //Instância do objeto RedeNeural
        private CSerieTemporal Serie;

        //Erros 
        public double learningMAPE = 0.0;        
        public double predictionMAPE = 0.0;
        public double learningUtheil = 0.0;
        public double predictionUtheil = 0.0;
        public double learningError = 0.0;
        public double predictionError = 0.0;
        private double validationError = 0.0;
        private double validationUtheil = 0.0;
        private double validationMAPE = 0.0;
        private double[] menoresErros = new double[3];
            
        //Configurações de rede
        private List<int> neurons = null;
        private ActivationNetwork network = null;
        private ActivationNetwork networkValidation = null;
        private int windowSize = 5;
        private int predictionSize = 1;

        #endregion



        #region Construtor

        public CRedeNeural() 
        {
            neurons = new List<int>();
        }

        #endregion



        #region Propriedades

        public ActivationNetwork Network 
        {
            get { return network; }
            set { if (network != value) { network = value; } } 
        }

        ///// <summary>
        ///// Algoritmo de previsão utilizado pela rede.
        ///// </summary>
        //public string Algoritmo { set; }

        /// <summary>
        /// Lista de neurônios distribuídos em camadas. Cada linha da lista indica uma camada.
        /// </summary>
        public List<int> Neuronios 
        { 
            get { return neurons; }
            set { if (neurons != value) { neurons = value; } } 
        }

        /// <summary>
        /// Tamanho da janela para trás (windowsize).
        /// </summary>
        public int WindowSize
        {
            get { return windowSize; }
            set { if (windowSize != value) { windowSize = value; } }
        }

        /// <summary>
        /// Valor da quantidade de pontos a serem previstos (predictionsize).
        /// </summary>
        public int PredictionSize
        {
            get { return predictionSize; }
            set { if (predictionSize != value) { predictionSize = value; } }
        }

        #endregion



        #region Métodos Públicos

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
            Serie = new CSerieTemporal(dados, xInicial);

            //Aplica a diferença nos dados e separa em treino, validação e teste.
            Serie.PrepararDados(true);

            dadosTreino = Serie.DadosTreino;
            dadosValidacao = Serie.DadosValidacao;
            dadosTeste = Serie.DadosTeste;

            //resultados
            List<double> resultTreino = new List<double>();        
            
            TestarConfiguracoes();

            resultTreino = Treino();
            Teste();

            result.AddRange(resultTreino);
            result.AddRange(resultValidation);
            result.AddRange(resultTeste);


            return result;
        }

        public List<double> Treino()
        {
            //setando o erro de comparação da validação em um valor extremo
            menoresErros[2] = 1000;

            //variável que conta a quantidade de iterações
            int iteration;

            //variável auxiliar que define quando o processo de treino acaba
            bool needToStop = false; 

            //fator de normalização
            fatorNormal = 2.0 / (Serie.Max - Serie.Min);

            //lista contendo todos os ids de 1 a 52
            List<int> ids = Serie.Ids;
            //variavel que possuirá o id binário
            int[] id = new int[6];

            // número de amostras para a aprendizagem
            int samples = dadosTreino.Count - windowSize - predictionSize;

            // preparação dos dados para a aprendizagem
            double[][] input = new double[samples][];//vetor de entrada
            double[][] output = new double[samples][];//vetor de saída

            for (int i = 0; i < samples; i++)
            {
                //define o tamanho da entrada 
                input[i] = new double[windowSize + predictionSize * 6];
                //define o tamanho da saída
                output[i] = new double[predictionSize];

                // configura a entrada com os dados formatados
                for (int j = 0; j < windowSize + predictionSize * 6; j++)
                {
                    if (j < windowSize)
                    {
                        input[i][j] = (dadosTreino[i + j] - Serie.Min) * fatorNormal - 1.0;
                    }
                    else
                    {
                        if (j == windowSize)
                            id = CUtil.ConversaoBinario(ids[i + windowSize]);

                        input[i][j] = id[j - windowSize];
                    }
                }// fim do for interno

                // configura os dados de saída com os dados transformados
                for (int k = 0; k < predictionSize; k++)
                {
                    output[i][k] = (dadosTreino[i + k + windowSize] - Serie.Min) * fatorNormal - 1;
                }
            }//fim do for externo

            //cria o "Professor" da rede neural
            ParallelResilientBackpropagationLearning teacher = new ParallelResilientBackpropagationLearning(network);

            //Variável para contar o número de iterações
            iteration = 1;

            //vetor que armazena a solução encontrada pela rede neural
            int solutionSize = dadosTreino.Count - windowSize;
            double[] solution = new double[solutionSize];

            //Vetor auxiliar que seta as entrada a serem computadas pela rede neural
            double[] networkInput = new double[windowSize + predictionSize * 6];

            // loop que efetua as iterações
            while (!needToStop)
            {
                learningError = 0.0;
                learningMAPE = 0.0;
                learningUtheil = 0.0;

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
                for (int i = 0, n = dadosTreino.Count - windowSize - predictionSize + 1; i < n; i++)
                {
                    int a = windowSize;
                    contador = 0;
                    // seta os valores da atual janela de previsão como entrada da rede neural
                    for (int j = 0; j < windowSize + predictionSize; j++)
                    {
                        if (j < windowSize)
                        {
                            //entrada tem de ser formatada
                            networkInput[j] = (dadosTreino[i + j] - Serie.Min) * fatorNormal - 1.0;
                        }
                        else
                        {
                            id = CUtil.ConversaoBinario(ids[i + a]);
                            a++;

                            for (int c = 0; c < 6; c++)
                            {
                                networkInput[windowSize + contador] = id[c];
                                contador++;
                            }
                        }//fim do else
                    }//fim do for interno                          

                    //computa a saída da rede e armazena o dado solution diferenca
                    for (int k = 0; k < network.Compute(networkInput).Length; k++)
                    {
                        double diferenca = (network.Compute(networkInput)[k] + 1.0) / fatorNormal + Serie.Min;
                        if ((i + k) < solutionSize) solution[i + k] = (diferenca) + Serie.Dados[windowSize + i, 1];
                    }

                    //calcula o erro de aprendizagem
                    amostra++;

                    //variaveis auxiliares do u theil
                    somaY += ((Serie.Dados[windowSize + i, 1]) * (Serie.Dados[windowSize + i, 1]));
                    somaF += ((solution[i] * solution[i]));

                    learningError += ((solution[i] - Serie.Dados[windowSize + i + 1, 1]) * (solution[i] - Serie.Dados[windowSize + i, 1]));

                }//fim do for externo

                learningError = (learningError) / amostra;
                somaF = somaF / amostra;
                somaY = somaY / amostra;

                learningUtheil = Math.Sqrt(learningError) / (Math.Sqrt(somaY) + Math.Sqrt(somaF));

                // validação
                if (iteration >= 30)
                    Validacao();

                // incrementa a iteração atual
                iteration++;

                // confere se precisamos ou não parar, fator de parada é o número de iterações
                if (iteration > 400)
                    break;
            }//fim do while

            validationError = menoresErros[0];
            validationMAPE = menoresErros[1];
            validationUtheil = menoresErros[2];

            return solution.ToList();
        }

        public void Teste()
        {
            predictionError = 0.0;
            predictionMAPE = 0.0;
            predictionUtheil = 0.0;

            double somaY = 0.0;
            double somaF = 0.0;

            int indice = dadosValidacao.Count + dadosTreino.Count;
            resultTeste = Prever(dadosTeste, dadosValidacao, indice);

            int tamanho = Serie.Dados.Length / 2 - dadosTeste.Count - 1;
            int amostra = 0;

            for (int i = 0; i < dadosTeste.Count + 1; i++)
            {
                if (Serie.Dados[tamanho + i, 1] != 0)
                {
                    predictionError += ((resultTeste[i] - Serie.Dados[tamanho + i, 1]) * (resultTeste[i] - Serie.Dados[tamanho + i, 1]));
                    predictionMAPE += Math.Abs(((Serie.Dados[tamanho + i, 1]) - resultTeste[i]) / (Serie.Dados[tamanho + i, 1]));
                    amostra++;
                    somaY += (Serie.Dados[tamanho + i, 1] * Serie.Dados[tamanho + i, 1]);
                    somaF += (resultTeste[i] * resultTeste[i]);
                }
            }

            predictionError = (predictionError) / amostra;
            predictionMAPE = predictionMAPE / amostra;
            somaF = somaF / amostra;
            somaY = somaY / amostra;

            predictionUtheil = Math.Sqrt(predictionError) / (Math.Sqrt(somaY) + Math.Sqrt(somaF));
        }

        public void configuraRede()
        {
            //Se houver algum 0 na lista de neurônios, remover.
            neurons.Remove(0);

            //O número de camadas é igual a quantidade de 
            int camadaIntermediaria = neurons.Count;

            int[] camadas = new int[camadaIntermediaria + 1];
            for (int i = 0; i < camadaIntermediaria; i++)
            {
                camadas[i] = neurons[i];
            }
            camadas[camadaIntermediaria] = predictionSize;

            network = new ActivationNetwork (new BipolarSigmoidFunction(2.0), windowSize + predictionSize*6, camadas);
            
            //randomizar os pesos
            NguyenWidrow weightRandom = new NguyenWidrow(network);
            for (int i = 0; i < network.Layers.Length; i++)
                weightRandom.Randomize(i);
        }

        #endregion



        #region Métodos Privados

        /// <summary>
        /// Faz a previsão de pontos inéditos a rede neural treinada.
        /// </summary>
        /// <param name="dadosBase">Dados a serem comparados com os previstos.</param>
        /// <param name="dadosAuxiliares">Dados prévios aos dados base. Para Validação: dados de treinamento. Para Teste: dados de validação.</param>
        /// <param name="indiceID">Indice em que se inicia os dados base em relação aos dados totais. Para validação: tamanho dos dados de treinamento. Para teste: tamanho dos dados de treinamento somado ao tamanho dos de teste.</param>
        /// <returns></returns>
        private List<double> Prever(List<double> dadosBase, List<double> dadosAuxiliares, int indiceID)
        {
            network = (ActivationNetwork)ActivationNetwork.Load(@"C:\Users\Paulo\Desktop\NetworkTest.bin");

            //criação da lista de dados provisória usada na previsão
            List<double> dadosPrevisao = new List<double>();

            List<double> diferenca = new List<double>();

            //lista contendo todos os ids de 1 a 52
            List<int> ids = Serie.Ids;
            //variavel que possuirá o id binário
            int[] id = new int[6];

            int tamanhoAux = (dadosAuxiliares.Count);

            //inicio do processo de adição de dados à lista fazendo que 
            //o primeiro ponto previsto seja exatamente o ultimo dos dados auxiliares
            int con = (dadosAuxiliares.Count) - windowSize - 1;

            for (int i = con; i < tamanhoAux; i++)
            {
                //adiciona os valosres de data, a lista de dados para treino primeiro
                dadosPrevisao.Add(dadosAuxiliares[i]);
            }

            //definição do tamanho da solução, deve ser do tamanho do teste mais um
            int solutionSize = dadosBase.Count + 1;
            List<double> solution = new List<double>();

            //definição do tamanho da entrada da rede neural para a previsão
            double[] networkInput = new double[windowSize + predictionSize * 6];

            //variavel auxiliar para o id binário
            int contador = 0;

            con = indiceID - windowSize - 1;

            //inicia processo de predição deslocando de um por um os pontos previstos
            for (int i = 0, n = dadosBase.Count + 1; i < n; i = i + predictionSize)
            {
                int a = windowSize;
                contador = 0;
                // seta os valores da atual janela de previsão como entrada da rede neural
                for (int j = 0; j < windowSize + predictionSize; j++)
                {
                    if (j < windowSize)
                    {
                        //entrada tem de ser formatada
                        networkInput[j] = (dadosPrevisao[i + j] - Serie.Min) * fatorNormal - 1.0;
                    }
                    else
                    {
                        id = CUtil.ConversaoBinario(ids[con + i + a]);
                        a++;

                        for (int c = 0; c < 6; c++)
                        {
                            networkInput[windowSize + contador] = id[c];
                            contador++;
                        }
                    }
                }//fim do for interno

                for (int k = 0; k < network.Compute(networkInput).Length; k++)
                {
                    if ((i + k) < solutionSize)
                    {
                        diferenca.Add((network.Compute(networkInput)[k] + 1.0) / fatorNormal + Serie.Min);
                        dadosPrevisao.Add((network.Compute(networkInput)[k] + 1.0) / fatorNormal + Serie.Min);
                    }
                }

            }//fim do for externo

            solution = Serie.DiferencaInversa(diferenca, Serie.Dados[indiceID, 1]);
            solution.RemoveAt(0);

            return solution;
        }

        private void Validacao()
        {
            validationError = 0.0;
            validationMAPE = 0.0;
            validationUtheil = 0.0;

            //variaveis auxiliares para o calculo do utheil
            double somaF = 0.0;
            double somaY = 0.0;

            //solução gerada como saída da rede neural para validação
            int indice = dadosTreino.Count;
            resultValidationTemp = Prever(dadosValidacao, dadosTreino, indice);

            //variável auxiliar sendo reutilizada para o calculo do erro
            int amostra = 0;

            for (int i = 0; i < dadosValidacao.Count + 1; i++)
            {
                //retirando os zeros do cálculo de erro        
                if (Serie.Dados[(dadosTreino.Count) + i, 1] != 0)
                {
                    //Os erros serão trabalhandos com y2, os dados reais de validação, e a solução da rede neural
                    validationMAPE += Math.Abs((resultValidationTemp[i] - Serie.Dados[(dadosTreino.Count) + i, 1]) / (Serie.Dados[(dadosTreino.Count) + i, 1]));
                    validationError += ((resultValidationTemp[i] - Serie.Dados[(dadosTreino.Count) + i, 1]) * (resultValidationTemp[i] - Serie.Dados[(dadosTreino.Count) + i, 1]));
                    somaY += (Serie.Dados[(dadosTreino.Count) + i, 1] * Serie.Dados[(dadosTreino.Count) + i, 1]);
                    somaF += (resultValidationTemp[i] * resultValidationTemp[i]);
                    amostra++;
                }
            }

            somaY = somaY / amostra;
            somaF = somaF / amostra;
            validationError = validationError / amostra;
            validationMAPE = validationMAPE / amostra;
            validationUtheil = (Math.Sqrt(validationError)) / (Math.Sqrt(somaY) + Math.Sqrt(somaF));

            if (validationUtheil < menoresErros[2])
            {
                menoresErros[0] = validationError;
                menoresErros[1] = validationMAPE;
                menoresErros[2] = validationUtheil;

                //Copia network
                networkValidation = CUtil.DeepCopy(network);

                //salva a solução da melhor configuração
                resultValidation = resultValidationTemp;
            }
        }

        private void TestarConfiguracoes()
        {
            // Limpa a rede neural anterior e reutiliza 
            network = null;
            neurons.Clear();

            neurons.Add(25);
            neurons.Add(15);
            predictionSize = 1;
            windowSize = 5;

            configuraRede();
        }        

        #endregion
    }
}
