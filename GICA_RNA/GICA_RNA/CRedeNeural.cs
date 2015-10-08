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

        private List<int> neurons = null;
        private ActivationNetwork network = null;
        private int windowsize;
        private int predictionsize;

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
            get { return windowsize; }
            set { if (windowsize != value) { windowsize = value; } }
        }

        /// <summary>
        /// Valor da quantidade de pontos a serem previstos (predictionsize).
        /// </summary>
        public int PredictionSize
        {
            get { return predictionsize; }
            set { if (predictionsize != value) { predictionsize = value; } }
        }

        public double[,] DadosTreino { get; set; }
        public double[,] DadosValidacao { get; set; }
        public double[,] DadosTeste { get; set; } 

        #endregion

        #region Métodos

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
            camadas[camadaIntermediaria] = predictionsize;

            network = new ActivationNetwork (new BipolarSigmoidFunction(2.0), windowsize + predictionsize*6, camadas);
            
            //randomizar os pesos
            NguyenWidrow weightRandom = new NguyenWidrow(network);
            for (int i = 0; i < network.Layers.Length; i++)
                weightRandom.Randomize(i);
        }

        #endregion
    }
}
