using System;
using System.Web.Script.Serialization;

namespace Appsettings.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("-------数据库APP配置---------");
            DBStringTest();

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------开始读取配置----------");

            Reload:

            try
            {
                Console.WriteLine();
                Console.WriteLine("当前时间:{0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));

                // aa.xml中的kv
                var str1 = AppSettingsManager.AppSettings["AAA"];
                Console.WriteLine("AAA: {0}", str1);

                // aa.xml中的属性读取
                var name = AppSettingsManager.GetAttributesValue("Person", "Name");
                var age = AppSettingsManager.GetAttributesValue("Person", "Age");
                var height = AppSettingsManager.GetAttributesValue("Person", "Height");
                Console.WriteLine("Person: Name={0}  Age={1}  Height={2}", name, age, height);

                // aa.xml中的对象读取
                var person = AppSettingsManager.GetEntity<Person>();
                Console.WriteLine("person: {0}", new JavaScriptSerializer().Serialize(person));

                // aa.xml中的集合读取
                // 参数xmlSubPath为xml中实体对应的路径
                var list = AppSettingsManager.GetEntityList<FaceMsg>("AppSettings.FaceMsgList2");
                Console.WriteLine("List<FaceMsg>: {0}个元素", list.Count);

                // config.properties中的kv
                var str2 = AppSettingsManager.AppSettings["fileSys.upload"];
                Console.WriteLine("fileSys.upload: {0}", str2);

                // Test.xml中的kv
                var str3 = AppSettingsManager.AppSettings["PostUrl"];
                Console.WriteLine("PostUrl: {0}", str3);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            var keys = Console.ReadKey();
            if (keys.Key != ConsoleKey.Escape)
            {
                goto Reload;
            }
        }

        static void DBStringTest()
        {
            string str = "Server = myServerAddress;Database = myDataBase;User ID = myUsername;Password = myPassword;Trusted_Connection = False;";
            str = AppSettingsManager.BuildDBConnString(str);
            Console.WriteLine("db string = {0}", str);

            str = "Data Source=10.1.20.57;Initial Catalog=EStandardAccountLog_V3;persist security info=True;MultipleActiveResultSets=true;user id=Finance_Admin;password=Finance_AdminAdmin;Max Pool Size=512;";
            str = AppSettingsManager.BuildDBConnString(str);
            Console.WriteLine("db string = {0}", str);

            str = "metadata=res://*/PortalModel.CreditPotal.csdl|res://*/PortalModel.CreditPotal.ssdl|res://*/PortalModel.CreditPotal.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=10.1.20.126;initial catalog=CreditFront;persist security info=True;user id=Finance_Admin;password=Finance_AdminAdmin;MultipleActiveResultSets=True;Max Pool Size=512&quot;";
            str = AppSettingsManager.BuildDBConnString(str);
            Console.WriteLine("db string = {0}", str);
            Console.WriteLine();
        }
    }

    public class FaceMsg
    {
        public string MsgKey { get; set; }
        public string MsgType { get; set; }
        public string MsgTip { get; set; }
    }
    public class Person
    {
        public string Name { get; set; }
        public string Name2 { get; set; }
        public int Age { get; set; }
        public int Height { get; set; }
        public decimal? Money { get; set; }
    }
}
