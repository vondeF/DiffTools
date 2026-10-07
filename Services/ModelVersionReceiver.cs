using Monitel.ObjectDb;
using Monitel.ObjectDb.Client.Rmq;
using Monitel.ObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Services
{
    public class ModelVersionReceiver
    {
        private OdbClient client;
        public ModelVersionReceiver()
        {
            client = OdbClientFactory.CreateClient();
        }

        /// <summary>
        /// Проверяет существование версии модели
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="databaseName"></param>
        /// <param name="versionNumber"></param>
        /// <returns></returns>
        public bool IsVersionExists(string serverName, string databaseName, int versionNumber)
        {
            var inst = client.GetInstanceByName(serverName, databaseName);
            var versionsList = client.GetAllModelVersions(inst).Select(x => x.Id);

            if (versionsList.Contains(versionNumber))
                return true;
            return false;
        }

        ///// <summary>
        ///// Возвращает модель, в которую ежедневно применяются наборы изменений из CIM-портала из контекста DB_Portal
        ///// </summary>
        ///// <returns></returns>
        //public int GetGostPortalVersion()
        //{
        //    var inst = svc.GetInstanceByName("ia-im-sipr1", App.GostPortalDatabase);
        //    return GetPortalVersion(inst);
        //}

        ///// <summary>
        ///// Возвращает модель, в которую ежедневно применяются наборы изменений из CIM-портала из контекста DB_Portal_nonGost
        ///// </summary>
        ///// <returns></returns>
        //public int GetNonGostPortalVersion()
        //{
        //    var inst = svc.GetInstanceByName("ia-im-sipr1", App.NonGostPortalDatabase);
        //    return GetPortalVersion(inst);
        //}

        private int GetPortalVersion(ObjectDatabase inst)
        {
            var dbList = client.GetAllModelVersions(inst);
            var actual = dbList.FirstOrDefault(x => x.IsActual);
            var children = dbList.Where(x => x.BaseModelVersionId == actual.Id);
            var toApply = children.FirstOrDefault(x => x.Description?.ToLower() == "4diff");
            if (toApply != null)
                return toApply.Id;
            return dbList.First(x => x.BaseModelVersionId == actual.Id).Id;
        }
    }
}

