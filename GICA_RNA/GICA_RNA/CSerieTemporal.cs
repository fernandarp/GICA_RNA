using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GICA_RNA
{
    class CSerieTemporal
    {
        #region Atributos

        private double[,] dadosXY;

        private List<double> dados = new List<double>();
        private List<int> ids = new List<int>();
        private List<double> dadosDiferenca = new List<double>();        
        private int xInicial = 1;

        //Variáveis auxiliares para normalização setados em valores extremos.
        private double max = -1000;
        private double min = 1000;

        //Variáveis de dados
        private List<double> dadosTreino = new List<double>();
        private List<double> dadosValidacao = new List<double>();
        private List<double> dadosTeste = new List<double>(); 
        
        #endregion



        #region Propriedades

        public double[,] Dados
        {
            get { return dadosXY; }
            set { if (dadosXY != value) dadosXY = value; }
        }

        public List<int> Ids
        {
            get { return ids; }
        }

        public double Max
        {
            get { return max; }
        }

        public double Min
        {
            get { return min; }
        }

        public List<double> DadosTreino
        {
            get { return dadosTreino; }
        }
        public List<double> DadosValidacao
        {
            get { return dadosValidacao; }
        }
        public List<double> DadosTeste
        {
            get { return dadosTeste; }
        }

        #endregion



        #region Construtor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dadosST"></param>
        /// <param name="xInicialST"></param>
        public CSerieTemporal(List<double> dadosST, int xInicialST = 1)
        {
            dadosXY = UpdateDados(dadosST, xInicialST);
            dados = dadosST;
            xInicial = xInicialST;
        }

        #endregion



        #region Métodos

        /// <summary>
        /// Organiza os dados no padrão utilizado pelo programa.
        /// </summary>
        /// <param name="dados">Dados em formato de lista.</param>
        /// <param name="xInicial">Valor do indice inicial dos dados.</param>
        /// <returns></returns>
        private double[,] UpdateDados(List<double> dados, int xInicial)
        {
            double[,] dadosTemp = new double[dados.Count, 2];
            int cont = 0;

            foreach (double d in dados)
            {
                dadosTemp[cont, 0] = xInicial;
                dadosTemp[cont, 1] = d;
                cont++;
                xInicial++;
            }

            return dadosTemp;
        }

        /// <summary>
        /// Adiciona pontos a série.
        /// </summary>
        /// <param name="pontos">Lista dos pontos a serem adicionados.</param>
        public void Add(List<double> pontos)
        {
            foreach (double p in pontos)
                dados.Add(p);

            dadosXY = UpdateDados(dados, xInicial);
        }

        /// <summary>
        /// Remove pontos da série.
        /// </summary>
        /// <param name="quantidade">Quantidade de pontos a serem removidos do final da série.</param>
        public void Remove(int posicaoInicial, int quantidade)
        {
            dados.RemoveRange(posicaoInicial, quantidade);
            dadosXY = UpdateDados(dados, xInicial);
        }

        private List<double> Diferenca(List<double> dadosDI)
        {
            List<double> temp = new List<double>();
            for (int d = 0; d < dadosDI.Count - 1; d++)
                temp.Add(dadosDI[d + 1] - dadosDI[d]);
            return temp;
        }
        
        /// <summary>
        /// Converte a diferença em valores reais.
        /// </summary>
        /// <param name="diferenca">Lista com os valores de diferença.</param>
        /// <param name="valorInicial">Primeiro valor real.</param>
        public List<double> DiferencaInversa(List<double> diferenca, double valorInicial)
        {
            List<double> temp = new List<double>();

            temp.Add(valorInicial);

            for (int i = 0; i < diferenca.Count; i++)
            {
                temp.Add(temp[i] + diferenca[i]);
            }

            return temp;
        }        
        
        /// <summary>
        /// Separa os dados em treino, teste e validação.
        /// </summary>
        public void PrepararDados(bool Teste = true)
        {
            //Aplica diferença
            dadosDiferenca = Diferenca(dados);

            //Variáveis auxiliares de tamanhos
            int tamanhoTreino;
            int tamanhoValidacao;

            //Valor inicial para o id
            int id = Convert.ToInt16(dadosXY[0, 0]);

            //Transforma em as variáveis [x, 0] nos ids de 1 a 52
            for (int i = 0; i < dadosDiferenca.Count; i++)
            {                
                ids.Add(id);
                id++;
                if (id > 52) id = 1;
            }

            //Se o teste existe, instancia DadosTeste
            if (Teste == true)
            {
                tamanhoTreino = (dadosDiferenca.Count) * 60 / 100;
                tamanhoValidacao = (dadosDiferenca.Count) * 20 / 100;
            }
            else
            {
                tamanhoTreino = (dadosDiferenca.Count) * 70 / 100;
                tamanhoValidacao = (dadosDiferenca.Count) * 30 / 100;
            }

            //carregamento a partir dos dados totais para dados de treino
            for (int i = 0; i < tamanhoTreino; i++)
            {
                dadosTreino.Add(dadosDiferenca[i]);
            }

            //carregamento a partir dos dados totais para dados de validacao
            for (int i = tamanhoTreino; i < tamanhoTreino + tamanhoValidacao; i++)
            {
                dadosValidacao.Add(dadosDiferenca[i]);
            }

            if (Teste == true)
            {
                //carregamento a partir dos dados totais para dados de validacao
                for (int i = tamanhoTreino + tamanhoValidacao; i < dadosDiferenca.Count; i++)
                {
                    dadosTeste.Add (dadosDiferenca[i]);
                }
            }

            max = dadosTreino.Max();
            min = dadosTreino.Min();

            if (dadosValidacao.Max() > max) max = dadosValidacao.Max();
            if (dadosValidacao.Min() < min) min = dadosValidacao.Min();
        }

        #endregion
    }
}
