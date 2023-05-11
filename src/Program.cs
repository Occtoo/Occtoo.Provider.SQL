using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace Occtoo.Provider.SQL
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var books = GetBooksFromDatabase();
            var booksDynamicEntities = GetEntitiesToOnboard(books);
            OnboardDataToOcctoo(booksDynamicEntities, "##DataProviderClientId##", "##DataProviderSecret##", "##OcctooSourceName##");
        }

        public static List<Books> GetBooksFromDatabase()
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)?.Replace("\\bin\\Debug\\net7.0", "");
            string _connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={path}\BooksDatabase.mdf;Integrated Security=True";
            var books = new List<Books>();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM [Table]", con)
                {
                    CommandType = CommandType.Text
                };
                con.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    books.Add(new Books
                    {
                        Id = rdr.GetInt32("Id"),
                        Author = rdr.GetString("Author"),
                        Description = rdr.GetString("Description"),
                        Price = rdr.GetDecimal("Price")
                    });
                }
            }

            return books;
        }

        public static List<DynamicEntity> GetEntitiesToOnboard(List<Books> books)
        {
            List<DynamicEntity> entities = new List<DynamicEntity>();
            foreach (var book in books)
            {
                DynamicEntity entity = new DynamicEntity
                {
                    Key = book.Id.ToString()
                };
                
                foreach (PropertyInfo propertyInfo in book.GetType().GetProperties())
                {
                    if (propertyInfo.Name != nameof(book.Id))
                    {
                        DynamicProperty property = new DynamicProperty();
                        property.Id = propertyInfo.Name;
                        property.Value = propertyInfo.GetValue(book, null)?.ToString();
                        entity.Properties.Add(property);
                    }
                }

                entities.Add(entity);
            }
            return entities;
        }

        public static void OnboardDataToOcctoo(List<DynamicEntity> entities, string dataProviderClientId, string dataProviderSecret, string occtooSourceName)
        {
            var onboardingServliceClient = new OnboardingServiceClient(dataProviderClientId, dataProviderSecret);
            var response = onboardingServliceClient.StartEntityImport(occtooSourceName, entities);
            if (response.StatusCode != 202)
            {
                throw new Exception($"Batch import was not successful - status code: {response.StatusCode}. {response.Message}");
            }

            Console.WriteLine($"{entities.Count} {occtooSourceName} got onboarded to Occtoo!");
        }
    }
}

