using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace GICA_RNA
{
    static class CUtil
    {
        /// <summary>
        /// Converte um valor id para um binário de 6 dígitos.
        /// </summary>
        /// <returns></returns>
        public static int[] ConversaoBinario(int id)
        {
            string tempBin = "";
            int[] idBin = new int[6];
          
            //Converte o id inteiro para base binária ('2') com '6' digitos. Completa com 0 a esquerda caso retorne um número com menos de 6 digitos.
            tempBin = Convert.ToString(id, 2).PadLeft(6, '0');

            for (int i = 0; i < 6; i++)
                idBin[i] = Convert.ToInt16(Char.GetNumericValue(tempBin[i]));

            return idBin;            
        }

        /// <summary>
        /// Copia um objeto sem que as mudanças da cópia reflitam no objeto original.
        /// 
        /// Deep copy is creating a new object and then copying the nonstatic fields of the current object to the new object. 
        /// If a field is a value type --> a bit-by-bit copy of the field is performed. 
        /// If a field is a reference type --> a new copy of the referred object is performed.
        /// </summary>
        /// <typeparam name="T">Tipo de extensão.</typeparam>
        /// <param name="other">Arquivo a ser copiado.</param>
        /// <returns></returns>
        public static T DeepCopy<T>(T other)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }

        /// <summary>
        /// Gera um object[] de double dentro de um determinado intervalo.
        /// </summary>
        /// <param name="min">O primeiro valor da sequência.</param>
        /// <param name="max">O último valor da sequência.</param>
        /// <param name="passo">O valor do passo.</param>
        /// <returns></returns>
        public static Object[] Listar(double min, double max, double passo)
        {
            int count = 0;

            for (double i = min; i <= max; i = i + passo) count++;

            Object[] lista = new object[Convert.ToInt16(count)];

            count = 0;
            for (double i = min; i <= max; i = i + passo)
            {
                lista[count] = i;
                count++;
            }

            return lista;
        }

        /// <summary>
        /// Retorna o produto cartesiado dos valores presentes na sequência dada.
        /// </summary>
        /// <param name="sequences">Sequência de qualquer tipo.</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences)
            {
                var localSequence = sequence;
                result = result.SelectMany(
                  _ => localSequence,
                  (seq, item) => seq.Concat(new[] { item })
                );
            }
            return result;
        }
    }
}
