using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR.Misc.Commands))]

namespace AR.Misc
{
   public class Commands
   {
      // Переименование блоков с именами вида - DET_АРКВ_14т - DET_АРКВ_14т-10723881-S_01, в АРКВ_14т
      [CommandMethod("PIK", "AR-RenameBlocksApartFromRevit", CommandFlags.Modal)]// | CommandFlags.NoBlockEditor)]
      public void RenameBlocksApartFromRevitCommand()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;
         try
         {
            using (var lockDoc = doc.LockDocument())
            {
               Model.RenameBlockApartFromRevit renamer = new Model.RenameBlockApartFromRevit();
               renamer.Rename();
            }
         }
         catch (System.Exception ex)
         {
            doc.Editor.WriteMessage("\n{0}", ex.ToString());
         }
      }
   }
}
