using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data.OleDb;

namespace GICA_RNA
{
    public partial class Form1 : Form
    {
        List<double> dadosreais = new List<double>();

        public Form1()
        {
            InitializeComponent();
        }

        private void TreinoButton_Click(object sender, EventArgs e)
        {
            CRedeNeural Previsao = new CRedeNeural();
            List<double> dadosprevistos = new List<double>();
            Dados();
            dadosprevistos = Previsao.Previsao(dadosreais);
        }

        private void Dados()
        {
            // novo dataset auxiliar na transferência
            DataSet dados = new DataSet();

            //************************************************************************************
            //Conexão com o arquivo criada
            OleDbConnection conexao = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" +

            "Data Source=" + @"C:\Users\Paulo\Dropbox\Grupo de Rede Neural\Fernanda\Caj-21L1.xlsx" + ";" +

            "Extended Properties='Excel 12.0 xml;HDR=YES';");

            //************************************************************************************

            // Adapta para a leitura do arquivo
            OleDbDataAdapter adapter = new OleDbDataAdapter("Select * From [Plan1$]", conexao);

            //Tenta extrair os dados, caso não uma mensagem de erro aparece
            try
            {
                conexao.Open();
                adapter.Fill(dados);
            }
            catch (Exception)
            {
                MessageBox.Show("Failed reading the file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                // fecha o arquivo após a extração
                if (conexao != null)
                    conexao.Close();
            }

            foreach (DataRow linha in dados.Tables[0].Rows)
            {
                double i;
                double h;
                //le o conteúdo de cada coluna do arquivo
                h = double.Parse(linha["semana"].ToString());//pega os dados da coluna que iniciA com o nome semana
                i = Convert.ToDouble(linha["Potência"].ToString());//pega os dados da coluna que inicia com o nome Potência

                dadosreais.Add(i);
            }
        }
    }
}
