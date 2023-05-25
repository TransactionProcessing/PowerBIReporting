using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIRelease.Business
{
    using System.Data.SqlClient;

    // TODO: Logging

    public interface IDatabaseManager
    {
        Task ExecuteScript(String connectionString, String sqlFilePath, CancellationToken cancellationToken);
    }

    public class DatabaseManager : IDatabaseManager{

        public async Task ExecuteScript(String connectionString,
                                        String sqlFilePath,
                                        CancellationToken cancellationToken){
            String fileName = Path.GetFileName(sqlFilePath);

            String content = File.ReadAllText(sqlFilePath, Encoding.Latin1);
            String[] commands = null;
            if (content.Contains("GO", StringComparison.CurrentCulture)){
                commands = content.Split("GO;");
            }
            else{
                commands = new String[1];
                commands[0] = content;
            }

            try{
                Program.writeNormal($"Running File {fileName}");

                using(SqlConnection connection = new SqlConnection(connectionString)){
                    await connection.OpenAsync(cancellationToken);
                    foreach (String commandText in commands){
                        using(SqlCommand command = new SqlCommand(commandText, connection)){
                            await command.ExecuteNonQueryAsync(cancellationToken);
                        }
                    }
                }

                Program.writePositive($"File {fileName} executed successfully");
            }
            catch(Exception ex){
                Program.writeNegative($"Error running File {fileName}{Environment.NewLine}{ex}");
                throw;
            }
        }
    }
}
