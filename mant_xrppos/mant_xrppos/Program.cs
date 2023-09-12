using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Diagnostics;
using System.Xml.Linq;
using System.Net;


//\\PRACTICANTE2\instalador
namespace comprimir
{
    public class Program
    {

        static void Main(string[] args)
        {

            Console.WriteLine("BBBBBBBBBBBB");
            ////byte YABUMISA;
            /*-----------------Conexion base de datos-------------------*/
            string connectionString = @"server=(local)\SQLEXPRESS;DATABASE=XRPPOS;User ID=SA; Password=Tersa2015";//;User ID=nombre_usuario;Password=contraseña_usuario;
            SqlConnection connection = new SqlConnection(connectionString);

            //Abrir la conexión a la base de datos
            connection.Open();
            // Reemplaza con el nombre correcto de tu tabla
            string tableName = "[dbo].[@XAMP_SIN_ArchivosSincTrace]";

            // Verificar si la tabla existe
            string checkTableExistsQuery = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {tableName}";

            //delete table 
            SqlCommand deleteTable = new SqlCommand(checkTableExistsQuery, connection);
            deleteTable.ExecuteNonQuery();

            //obtener nombre del log 
            string nombreLog;
            string logName = "SELECT name FROM sys.database_files WHERE type = 1";
            using (SqlConnection connectionLog = new SqlConnection(connectionString))

            using (SqlCommand sqlCommand = new SqlCommand(logName, connection))
            {
                nombreLog = (string)sqlCommand.ExecuteScalar();
                // Ahora la variable nombreLog contiene el nombre del archivo de registro de transacciones

                // Puedes utilizar la variable nombreLog para realizar otras operaciones
            }

            SqlCommand commandLog = new SqlCommand(@"ALTER DATABASE XRPPOS SET RECOVERY SIMPLE; DBCC SHRINKFILE (" + nombreLog + ", 1); ALTER DATABASE XRPPOS SET RECOVERY FULL;", connection);
            // Ejecutar el comando SQL
            commandLog.CommandTimeout = 1060;
            commandLog.ExecuteNonQuery();

            /*-------------------------------EJECUTAR SCRIPT SQL PARA MANT-----------------------*/
            // Leer el script SQL desde un archivo
            string script = File.ReadAllText(@"C:\XRPPOS\Backups\MANTENIMIENTO XRPPOS.SQL"); // Ruta para leer el archivo SQL

            // Crear un comando SQL y asignar el script y la conexión
            SqlCommand commandQ = new SqlCommand(script, connection);

            commandQ.CommandTimeout = 480; // Establecer el tiempo de espera a 60 segundos

            // Ejecutar el script SQL
            commandQ.ExecuteNonQuery();

            /*------------------------Creacion del backuo -------------*/
            // Crear un comando SQL y asignar el comando BACKUP DATABASE y la conexión
            SqlCommand command = new SqlCommand(@"BACKUP DATABASE XRPPOS TO DISK='C:\XRPPOS\Backups\XRPPOS.bak' WITH FORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 10", connection);
            // Ejecutar el comando SQL
            command.CommandTimeout = 480;
            command.ExecuteNonQuery();

            /*-------------------------------Cambiar nombre-----------------------*/
            // cambiar nombre con fecha y nombre tienda
            string dat3 = DateTime.Today.ToString("dd-MM-yyyy");
            string name;
            string query = "SELECT  [Name]  FROM [XRPPOS].[dbo].[@XAMPSETPOS]";

            using (SqlConnection connectionSelect = new SqlConnection(connectionString))
            using (SqlCommand commandN = new SqlCommand(query, connection))
            {
                name = (string)commandN.ExecuteScalar();
            }



            string rutaArchivoAntiguo = @"C:\XRPPOS\Backups\XRPPOS.bak";// path to read the old file
            string rutaArchivoNuevo = @"C:\XRPPOS\Backups\XRPPOS-" + dat3 + "-" + name + ".bak"; // new path yo save 

            // Cambiar el nombre del archivo

            if (File.Exists(rutaArchivoAntiguo))
            {
                if (File.Exists(rutaArchivoNuevo))
                {
                    File.Delete(rutaArchivoNuevo); // Eliminar el archivo existente en la nueva ubicación
                }
                File.Move(rutaArchivoAntiguo, rutaArchivoNuevo); // Mover el archivo y sobrescribir si ya existe
            }

            // Ruta del archivo de backup
            string filePath = rutaArchivoNuevo;


            /*-------------------------------COMMPRIMIR-----------------------*/
            // Ruta del archivo comprimido
            string compressedFilePath = @"C:\XRPPOS\Backups\XRPPOS-" + dat3 + "-" + name + ".zip"; // path to save zip file

            // Comprimir el archivo .bak
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                using (var archive = new ZipArchive(new FileStream(compressedFilePath, FileMode.Create), ZipArchiveMode.Create))
                {
                    var zipEntry = archive.CreateEntry(Path.GetFileName(filePath), CompressionLevel.Optimal);
                    using (var entryStream = zipEntry.Open())
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }



            /*-------------------------------EJECUTAR SCRIPT SQL PARA MANT-----------------------*/
            // Leer el script SQL desde un archivo

            // Crear un comando SQL y asignar el script y la conexión
            // Ejecutar el script SQL
            commandQ.CommandTimeout = 480;
            commandQ.ExecuteNonQuery();

            commandLog.ExecuteNonQuery();
            // Cerrar la conexión a la base de datos
            commandLog.ExecuteNonQuery();
            eliminarBK();
            enviearBK();
            Console.ReadKey();
            connection.Close();


        }

        static void eliminarBK()
        {
            string folderPath = @"C:\XRPPOS\Backups"; // Ruta de la carpeta donde se encuentran los archivos

            DateTime fechaHoy = DateTime.Today;
            DateTime fechaLimite = fechaHoy.AddDays(-1); // Obtener la fecha de ayer

            DirectoryInfo directory = new DirectoryInfo(folderPath);
            FileInfo[] files = directory.GetFiles();

            foreach (FileInfo file in files)
            {
                string extension = file.Extension.ToLower();
                if (file.LastWriteTime.Date <= fechaLimite && (extension == ".bak" || extension == ".zip"))
                {
                    try
                    {
                        file.Delete();
                        Console.WriteLine($"Archivo eliminado: {file.Name}");
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"Error al eliminar el archivo {file.Name}: {e.Message}");
                    }
                }

            }

        }

        static void enviearBK()
        {

            string connectionString = @"server=(local)\SQLEXPRESS;DATABASE=XRPPOS;User ID=SA; Password=Tersa2015";//;User ID=nombre_usuario;Password=contraseña_usuario;
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            string dat3 = DateTime.Today.ToString("dd-MM-yyyy");
            string name;
            string query = "SELECT  [Name]  FROM [XRPPOS].[dbo].[@XAMPSETPOS]";

            using (SqlConnection connectionSelect = new SqlConnection(connectionString))
            using (SqlCommand commandN = new SqlCommand(query, connection))
            {
                name = (string)commandN.ExecuteScalar();
            }

            string filePath = @"C:\XRPPOS\Backups\XRPPOS-" + dat3 + "-" + name + ".zip";
            string ftpServer = "ftp://192.168.1.118/";
            string userName = Environment.UserName; // Obtener el nombre de usuario de la computadora
            string password = "Tersa123";

            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(userName, password);
                client.UploadFile(ftpServer + Path.GetFileName(filePath), "STOR", filePath);
            }

            Console.WriteLine("Archivo transferido con éxito.");
            connection.Close();
        }

    }
}
