using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCAD_PIK_Manager;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR.Misc.Commands))]

namespace AR.Misc
{
   public class Commands
   {
      // Подготовка задания конструкторам для создания монтажек
      [CommandMethod("PIK", "AR-PrepareJobMountingForConstructor", CommandFlags.Modal)]// | CommandFlags.NoBlockEditor)]
      public void PrepareJobMountingForConstructorCommand()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;
         try
         {
            using (var lockDoc = doc.LockDocument())
            {
               Model.PrepareJobMountingForConstructor renamer = new Model.PrepareJobMountingForConstructor();
               renamer.Prepare();               
            }
         }
         catch (System.Exception ex)
         {
            doc.Editor.WriteMessage("\n{0}", ex.ToString());
            Log.Error(ex, "команда AR-PrepareJobMountingForConstructor");
         }
      }
   }
}
