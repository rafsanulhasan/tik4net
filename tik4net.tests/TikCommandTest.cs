﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tik4net.Objects;
using tik4net.Objects.Ip;

namespace tik4net.tests
{
    [TestClass]
    public class TikCommandTest : TestBase
    {
        private void DeleteAllItems(string itemsPath)
        {
            foreach (var id in Connection.CallCommandSync($"{itemsPath}/print").OfType<ITikReSentence>().Select(sentence => sentence.GetId()))
            {
                var deleteCommand = Connection.CreateCommandAndParameters($"{itemsPath}/remove", TikSpecialProperties.Id, id);
                deleteCommand.ExecuteNonQuery();
            }
        }


        [TestMethod]
        public void ExecuteNonQuery_Create_New_PPP_Object_Will_Not_Fail()
        {
            const string TEST_NAME = "test-name";
            const string PATH = "/ppp/secret";

            DeleteAllItems(PATH);
            var createCommand = Connection.CreateCommandAndParameters("/ppp/secret/add",
                "name", TEST_NAME);

            createCommand.ExecuteNonQuery();

            //cleanup
            DeleteAllItems("/ppp/secret");
        }

        [TestMethod]
        public void ExecuteNonQuery_Disable_PPP_Object_Will_Not_Fail()
        {
            const string TEST_NAME = "test-name";
            const string PATH = "/ppp/secret";

            DeleteAllItems(PATH);
            var createCommand = Connection.CreateCommandAndParameters("/ppp/secret/add",
                "name", TEST_NAME);
            createCommand.ExecuteNonQuery();

            var updateCommand = Connection.CreateCommandAndParameters("/ppp/secret/set",
                "disabled", "yes",
                TikSpecialProperties.Id, TEST_NAME);
            updateCommand.ExecuteNonQuery();

            //cleanup
            DeleteAllItems("/ppp/secret");
        }

        [TestMethod]
        public void ExecuteNonQuery_Add_And_Remove_IPAddress_Will_Not_Fail()
        {
            const string IP = "192.168.1.1/24";
            const string INTERFACE = "ether1";

            //create IP
            var createCommand = Connection.CreateCommandAndParameters("/ip/address/add",
                "interface", INTERFACE,
                "address", IP);
            createCommand.ExecuteNonQuery();

            //find our IP
            var id = Connection.CallCommandSync("/ip/address/print", $"?=address={IP}").OfType<ITikReSentence>().Single().GetResponseField(TikSpecialProperties.Id);

            //delete by ID
            var deleteCommand = Connection.CreateCommandAndParameters("/ip/address/remove",
                TikSpecialProperties.Id, id);
            deleteCommand.ExecuteNonQuery();
        }

        [TestMethod]
        public void ExecuteNonQuery_Update_Interface_Via_Name_In_Id_Will_Not_Fail()
        {
            //const string IP = "192.168.1.1/24";
            const string INTERFACE = "wlan1";

            //update interface name
            var updateCommand = Connection.CreateCommandAndParameters("/interface/wireless/set",
                "ssid", "test_ssid",
                ".id", INTERFACE);
            updateCommand.ExecuteNonQuery();
        }

        [TestMethod]
        public void ExecuteSingleRow_With_Tag_Parameter_Will_Not_HangUp_Or_Fail()
        {
            var command = Connection.CreateCommandAndParameters("/system/health/print", TikSpecialProperties.Tag, "1234");
            command.ExecuteSingleRow();
        }

        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(System.IO.IOException))]
        public void AsyncExecuteClosed_AfterReboot_AndNextCommandThrowsException()
        {
            var torchAsyncCmd = Connection.LoadAsync<Objects.Tool.ToolTorch>(t => { ; },
                null, 
                Connection.CreateParameter("interface", "ether1"));

            Thread.Sleep(3000);
            Connection.ExecuteNonQuery("/system/reboot");
            Thread.Sleep(3000);

            Assert.IsFalse(torchAsyncCmd.IsRunning);

            Connection.ExecuteScalar("/system/identity/print"); // throws IO exception (rebooted router)
        }

        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(System.IO.IOException))]
        public void AsyncExecuteWithDurationExecuteClosed_AfterReboot_AndNextCommandThrowsException()
        {
            var torchCommand = Connection.CreateCommandAndParameters("/tool/torch", "interface", "ether1");

            new Thread(() =>
            {
                Thread.Sleep(1000);
                Connection.ExecuteNonQuery("/system/reboot");
            }).Start();
            var result = torchCommand.ExecuteListWithDuration(20);
            Thread.Sleep(3000);
                    
            Assert.IsFalse(torchCommand.IsRunning);

            Connection.ExecuteScalar("/system/identity/print"); // throws IO exception (rebooted router)
        }

        [TestMethod]
        public void InvalidCommandThrowsExceptionButConnectionRemainsOpened()
        {
            Exception thrownException = null;
            try
            {
                Connection.ExecuteNonQuery("blah blah");
            }
            catch (Exception ex)
            {
                thrownException = ex;
            }

            Assert.IsNotNull(thrownException);
            Assert.IsTrue(Connection.IsOpened);
            var result = Connection.ExecuteScalar("/system/identity/print");
            Assert.IsNotNull(result);
        }
    }
}

//http://forum.mikrotik.com/viewtopic.php?t=88694
//http://wiki.microtik.com/viewtopic.php?f=9&p=576978