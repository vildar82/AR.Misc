using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR.Misc.Model.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace AR.Misc.Model
{
   // Переименование блоков с именами вида - DET_АРКВ_14т - DET_АРКВ_14т-10723881-S_01, в АРКВ_14т
   public class RenameBlockApartFromRevit
   {
      private readonly string _prefixBlApart = "DET_";
      private readonly string _dash = " - ";

      public void Rename()
      {
         Inspector.Clear();
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         Editor ed = doc.Editor;
         using (var t = db.TransactionManager.StartTransaction())
         {
            var bt = t.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

            foreach (ObjectId idBtr in bt)
            {
               var btr = t.GetObject(idBtr, OpenMode.ForRead) as BlockTableRecord;
               string oldName = btr.Name;
               if (!btr.IsLayout)
               {
                  string newName = string.Empty;
                  if (tryGetRenameName(btr.Name, out newName))
                  {
                     // Проверка нет ли уже такого имени блока в базе чертежа
                     if (string.IsNullOrWhiteSpace(newName))
                     {
                        Inspector.AddError(string.Format("{0} - при переименовании этого блока, новое имя имеет недопустимое значение = {1}", btr.Name, newName));
                     }
                     else
                     {
                        try
                        {
                           SymbolUtilityServices.ValidateSymbolName(newName, false);
                           if (bt.Has(newName))
                           {
                              Inspector.AddError(string.Format("{0} - переименнованный блок уже есть в чертеже - {1}", btr.Name, newName));
                           }
                           else
                           {                              
                              btr.UpgradeOpen();
                              btr.Name = newName;
                              ed.WriteMessage("\nБлок {0} переименован в {1}", oldName, newName);
                           }
                        }
                        catch
                        {
                           Inspector.AddError(string.Format("{0} - переименнованный блок имеет недопустимое имя {1}", btr.Name, newName));
                        }                        
                     }                     
                  }
               }
            }
            t.Commit();
         }
         if (Inspector.HasError)
         {
            Inspector.Show(); 
         }
      }

      private bool tryGetRenameName(string name, out string newName)
      {
         bool isApartBl = false;
         newName = string.Empty;
         if (name.StartsWith(_prefixBlApart))
         {
            var indexDash = name.IndexOf(_dash);
            if (indexDash <= 0)
            {
               Inspector.AddError(string.Format("{0} - имя блока начинается с {1}, но нет {2}.", name, _prefixBlApart, _dash));
            }
            newName = name.Substring(_prefixBlApart.Length, indexDash-_dash.Length).Trim();
            isApartBl = true;
         }
         return isApartBl;
      }
   }
}
