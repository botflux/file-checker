using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileChecker
{
    public class FileInformation
    {
        /// <summary>
        /// Initialize a new instance of FileInforation
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="type">Type</param>
        /// <param name="id">Id</param>
        public FileInformation(int year, string type, int id)
        {
            Year = year;
            Type = type;
            Id = id;
        }

        /// <summary>
        /// Represents the year
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// Represents the type
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Represents the id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Represents the filename
        /// </summary>
        public string FullName
        {
            get
            {
                return this.ToString();
            }
        }

        /// <summary>
        /// Returns a FileInformation object from a filename using the pattern "yyyytiii".
        /// yyyy: Année
        /// t: Type
        /// iii: Identifiant
        /// 
        /// </summary>
        /// <param name="filename">Nom du fichier</param>
        /// <returns></returns>
        public static FileInformation GetFileInformation(string filename)
        {
            Regex rx = new Regex(@"(?<year>\d{4})(?<type>\w)(?<id>\d{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Match m = rx.Match(filename);

            int year = int.Parse(m.Groups["year"].Value);
            int id = int.Parse(m.Groups["id"].Value);
            string type = m.Groups["type"].Value;

            return new FileInformation(year, type, id);
        }

        /// <summary>
        /// Returns the filename from this instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}{1}{2}", Year, Type, Id);
        }
    }
}
