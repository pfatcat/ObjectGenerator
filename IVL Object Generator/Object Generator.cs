using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Threading; 

namespace Object_Generator
{
    public partial class ObjectGenerator : Form
    {
        //private const string strConn = "Data Source=10.10.6.53;Initial Catalog=Master;User Id=MidasWeb;Password=tRUbraz8;";
        //private SqlConnection sqlConn = new SqlConnection(tbConnString.Text);


        private SqlConnection sqlConn
        {
            get
            {
                string server = tbServer.Text != "" ? tbServer.Text : ".";
                string database = tbDatabase.Text != "" ? tbDatabase.Text : "IVLDEV"; //or IVLDEV
                string userName = tbUserName.Text != "" ? tbUserName.Text : "chuckpfaff";
                string password = tbPassword.Text != "" ? tbPassword.Text : "p@ssw0rd1";

                string strConn = String.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", server, database, userName, password);
                SqlConnection _sqlConn = new SqlConnection(strConn);
                return _sqlConn;
            }
        }

        public ObjectGenerator()
        {
            InitializeComponent();

            populateControls();
        }

        public void populateControls()
        {
            try
            {
                /*tbServer.Text = "10.10.6.53";
                tbDatabase.Text = "CHLive_Dev";
                tbUserName.Text = "";
                tbPassword.Text = "";*/

                SqlDataAdapter sqlDA = new SqlDataAdapter("SELECT object_id, name FROM sys.tables ORDER BY name", sqlConn);

                DataSet dsTables = new DataSet();
                sqlDA.Fill(dsTables);

                BindingSource bs = new BindingSource();
                bs.DataSource = dsTables.Tables[0];
                cbTables.DataSource = bs.DataSource;
                cbTables.DisplayMember = "name";
                cbTables.ValueMember = "object_id";

                List<Item> Objects = new List<Item>();
                Objects.Add(new Item("Business Object", 1));
                Objects.Add(new Item("Repository Object", 2)); 
                Objects.Add(new Item("Business Logic Object", 3));
                Objects.Add(new Item("Factories", 4));

                cbObject.DataSource = Objects;
            }
            catch(Exception ex)
            {
                String x = String.Format(@"Exception occurred in populateControls()
                                            Exception Message: {0}
                                            Stack Trace: {1}", ex.Message, ex.StackTrace);
                tbOutput.Clear();
                tbOutput.Text = x;

            }
        }

        private void btnGenerateObjects_Click(object sender, EventArgs e)
        {
            try
             {
                tbOutput.Clear();

                String sql = String.Format(@"SELECT c.Column_Name,
                    c.Data_Type, 
                    c.IS_NULLABLE,
                    CASE WHEN cu.constraint_name IS NULL THEN 0 ELSE 1 end as IsPrimaryKey,
                    CASE WHEN cuf.constraint_name IS NULL THEN 0 ELSE 1 end as IsForeignKey,
                    p.TABLE_NAME as PrimaryKeyTable, 
                    p.COLUMN_NAME as PrimaryKeyColumn
                    FROM Information_Schema.Columns c
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
	                    ON c.table_name = cu.table_name
	                    AND c.Column_Name = cu.Column_Name
	                    AND OBJECTPROPERTY(OBJECT_ID(cu.constraint_name), 'IsPrimaryKey') = 1
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE cuf
	                    ON c.table_name = cuf.table_name
	                    AND c.Column_Name = cuf.Column_Name
	                    AND OBJECTPROPERTY(OBJECT_ID(cuf.constraint_name), 'IsForeignKey') = 1
                    LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	                    ON cuf.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
                    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE p
	                    on rc.UNIQUE_CONSTRAINT_NAME = p.CONSTRAINT_NAME
                    WHERE c.table_name = '{0}'", cbTables.Text);

                SqlDataAdapter sqlDA = new SqlDataAdapter(sql, sqlConn);
                DataSet dsColumns = new DataSet();
                sqlDA.Fill(dsColumns);

                switch (cbObject.Text)
                {

                    case "Business Object":
                        tbOutput.Text = GenerateBusinessObject(cbTables.Text, dsColumns.Tables[0]);
                        break;

                    case "Repository Object":
                        tbOutput.Text = GenerateRepositoryObject(cbTables.Text, dsColumns.Tables[0]);
                        break;

                    case "Business Logic Object":
                        tbOutput.Text = GenerateBusinessLogicObject(cbTables.Text, dsColumns.Tables[0]);
                        break;

                    case "Factories":
                        tbOutput.Text = GenerateFactoryCode(cbTables.Text, dsColumns.Tables[0]);
                        break;

                    default:
                        tbOutput.Text = "Unimplemented option";
                        break;
                }

             }
             catch(Exception ex)
            {
                String x = String.Format(@"Exception occurred in btnGenerateObjects_Click()
                                            Exception Message: {0}
                                            Stack Trace: {1}", ex.Message, ex.StackTrace);
                tbOutput.Clear();
                tbOutput.Text = x;

            }

            
        }


        private string GenerateBusinessObject(string tableName, DataTable dtColumns)
        {
            StringBuilder sbModel = new StringBuilder();
            sbModel.AppendFormat("/*************************   Auto Generated Model Code For {0} table     Generated @ {1} *************************/", tableName, DateTime.Now.ToString());
            sbModel.AppendLine();

            sbModel.AppendFormat(@"

using System;
using System.Collections.Generic;

              namespace BusinessObjects
              {{
                  public class {0}
                  {{
                    #region DatabaseFields
", tableName);
                sbModel.AppendLine();

                foreach(DataRow dr in dtColumns.Rows)
                {
                    bool isNullable = dr["IS_NULLABLE"].ToString() == "YES";

                    sbModel.AppendFormat("                     public  {0} {1} {{get; set;}}", ConvertType(dr["Data_Type"].ToString(), isNullable), dr["Column_Name"].ToString());                  
                    sbModel.AppendLine();

                }

                sbModel.AppendFormat(@"
                    #endregion


                    #region NonDatabaseProperties



                    #endregion
                  }}
              }}", tableName, tableName.ToLower());

            return sbModel.ToString();
        }


        private string GenerateBusinessLogicObject(string tableName, DataTable dtColumns)
        {
            StringBuilder sbModel = new StringBuilder();
            sbModel.AppendFormat("/*************************   Auto Generated Model Code For {0} table     Generated @ {1} *************************/", tableName, DateTime.Now.ToString());
            sbModel.AppendLine();

            sbModel.AppendFormat(@"

using System;
using System.Collections.Generic;
using DataRepository;

              namespace BusinessLogic
              {{
                  public class {0}Logic
                  {{

                    private {0}Repo repo;

                    //constructor
                    public {0}Logic({0}Repo _repo)
                    {{
                        repo = _repo;
                    }}


                    public {0} Get{0}ByID(int {0}ID)
                    {{
                    
                        return repo.Get{0}ByID({0}ID);
                    }}


                    public List<{0}> Get{0}sByExternalID(int ExternalID)
                    {{
                    
                        return repo.Get{0}ByExternalID(ExternalID);
                    }}

                  }}
              }}


", tableName, tableName.ToLower());

            return sbModel.ToString();
        }


        

        private string GenerateRepositoryObject(string tableName, DataTable dtColumns)
        {

            StringBuilder sbRepository = new StringBuilder();

            sbRepository.AppendFormat("/*************************   Auto Generated Repository Code For {0} table     Generated @ {1} *************************/", tableName, DateTime.Now.ToString());
            sbRepository.AppendLine();

            DataRow[] rows = dtColumns.Select();
            DataRow primaryKey = rows.FirstOrDefault(x => x["IsPrimaryKey"].ToString() == "1");

            string primaryKeyField = "ID";

            if (primaryKey != null)
            {
                primaryKeyField = primaryKey["Column_Name"].ToString();

            }

            StringBuilder sbDataMapping = new StringBuilder();
            foreach (DataRow dr in dtColumns.Rows)
            {

                bool isNullable = dr["IS_NULLABLE"].ToString() == "YES";
                sbDataMapping.AppendFormat(@"                           obj{2}.{1} = db.Translate<{0}>(dr, ""{1}"");{3}", ConvertType(dr["Data_Type"].ToString(), isNullable), dr["Column_Name"], tableName, System.Environment.NewLine); 
            }

            StringBuilder sbBusinessMapping = new StringBuilder();
            foreach (DataRow dr in dtColumns.Rows)
            {
                sbBusinessMapping.AppendFormat(@"                           ht.Add(""@{0}"",  obj{1}.{0});{2}", dr["Column_Name"], tableName, System.Environment.NewLine);
            }

            sbRepository.AppendFormat(@"

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BusinessObjects;

              namespace DataRepository
              {{
                  public class {0}Repo
                  {{


                    DataAccess db;
                    private IBusinessFactory _businessFactory;

                    public {0}Repo(IBusinessFactory businessFactory)
                    {{
                        db = new DataAccess();
                        _businessFactory = businessFactory;
                    }}

                    public {0} Get{0}ByID(int {0}ID)
                    {{
                        string sql = ""SELECT * FROM {0} WHERE {0}ID = @{0}ID"";

                        Hashtable ht = new Hashtable();
                        ht.Add(""@{0}ID"", {0}ID);

                        DataTable dt = db.executeQuery(sql, db.productionConn, false, ht);

                        if (dt == null || dt.Rows.Count == 0)
                        {{
                            return null;
                        }}

                        return ConvertToBusinessObject(dt.Rows[0]);
                    }}


                    public List<{0}> Get{0}sByExternalID(int ExternalID)
                        {{
                            string sql = ""SELECT * FROM {0} WHERE ExternalID = @ExternalID"";

                            Hashtable ht = new Hashtable();
                            ht.Add(""@ExternalID"", ExternalID);

                            DataTable dt = db.executeQuery(sql, db.productionConn, false, ht);

                            if (dt == null || dt.Rows.Count == 0)
                            {{
                                return null;
                            }}

                            var {2} = _businessFactory.Get{0}List();

                            foreach (DataRow dr in dt.Rows)
                            {{
                                {2}.Add(ConvertToBusinessObject(dr));

                            }}

                            return {2};
                        }}



                    
                    /// <summary>
                    /// Converts a {0} data object to a {0} business object
                    /// </summary>
                    /// <returns>A business object of type <see cref=""{0}""/></returns>
                    public {0} ConvertToBusinessObject(DataRow dr)
                    {{
                         var obj{0} = _businessFactory.Get{0}();

{3}

                        return obj{0};

                    }}
                    
                    /// <summary>
                    /// Converts a {0} business object to a hashtable of parameters
                    /// </summary>
                    /// <returns>A data object of type <see cref=""{0}""/></returns>
                    public Hashtable ConvertToHashtable(BusinessObjects.{0} obj{0})
                    {{
                        Hashtable ht = new Hashtable();

{4}

                        return ht;
                    }}
                }}
            }}


", tableName, primaryKeyField, tableName.ToLower(), sbDataMapping.ToString(), sbBusinessMapping.ToString());
           

            return sbRepository.ToString();
        }


        private string GenerateFactoryCode(string tableName, DataTable dtColumns)
        {

            StringBuilder sbModel = new StringBuilder();
            sbModel.AppendFormat("/*************************   Auto Generated Model Code For {0} table     Generated @ {1} *************************/", tableName, DateTime.Now.ToString());
            sbModel.AppendLine();
            sbModel.AppendLine();
            sbModel.AppendLine();

            sbModel.AppendFormat(@"
                ***************DATA FACTORY ******************

                public {0}Repo Get{0}Repo()
                {{
                    return new {0}Repo(_businessFactory);
                }}

                public {0}Repo Get{0}Repo()
                {{
                    return new {0}Repo(_businessFactory);
                }}



                ***************BUSINESS OBJECT FACTORY ******************

                public {0} Get{0}()
                {{
                    return new {0}();
                }}

                public {0} Get{0}List()
                {{
                    return new List<{0}>();
                }}


                ***************BUSINESS LOGIC FACTORY ******************

                public {0}Logic Get{0}Logic()
                {{
                    return new {0}Logic(_repoFactory.Get{0}Repo());
                }}
                ", tableName);


            return sbModel.ToString();

        }



        private string ConvertType(string SQLType, bool isNullable)
        {
            string dotnetType;

            switch(SQLType)
            {

                case "int":
                    dotnetType = isNullable ? "int?" : "int";
                    break;

                case "smallint":
                    dotnetType = isNullable ? "int?" : "int";
                    break;

                case "bigint":
                    dotnetType = isNullable ? "int?" : "int";
                    break;

                case "tinyint":
                    dotnetType = isNullable ? "int?" : "int";
                    break;

                case "bit":
                    dotnetType = isNullable ? "bool?" : "bool";
                    break;

                case "datetime":
                    dotnetType = isNullable ? "DateTime?" : "DateTime";
                    break;

                case "smalldatetime":
                    dotnetType = isNullable ? "DateTime?" : "DateTime";
                    break;

                case "decimal":
                    dotnetType = isNullable ? "Decimal?" : "Decimal";
                    break;

                case "float":
                    dotnetType = isNullable ? "float?" : "float";
                    break;

                case "money":
                    dotnetType = isNullable ? "Decimal?" : "Decimal";
                    break;

                case "uniqueidentifier":
                    dotnetType = isNullable ? "Guid?" : "Guid";
                    break;

                case "char":
                     dotnetType = isNullable ? "char?" : "char";
                    break;

                default:
                     dotnetType = "string";
                    break;

                    
            }
            
            return dotnetType;
        }

        // Content item for the combo box
        private class Item
        {
            public string Name;
            public int Value;
            public Item(string name, int value)
            {
                Name = name; Value = value;
            }
            public override string ToString()
            {
                // Generates the text shown in the combo box
                return Name;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            populateControls();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(tbOutput.Text);
        }

     }
}
