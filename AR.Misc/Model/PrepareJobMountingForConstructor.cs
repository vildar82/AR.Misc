using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR.Misc.Model.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AR.Misc.Model
{
   // Подготовка задания для конструкторов
   public class PrepareJobMountingForConstructor
   {
      private Document _doc;
      private Database _db;
      private Editor _ed;

      private readonly string _prefixBlApart = "DET_";
      private readonly string _dash = " - ";
      private List<string> _layersIncluded; // список включенных слоев, остальные слои будут выключены

      public PrepareJobMountingForConstructor()
      {
         _doc = Application.DocumentManager.MdiActiveDocument;
         _db = _doc.Database;
         _ed = _doc.Editor;
         _layersIncluded = new List<string>() { "Обобщенные модели", "Сетки" };
      }

      // Переименование блоков с именами вида - DET_АРКВ_14т - DET_АРКВ_14т-10723881-S_01, в АРКВ_14т
      private void renameApartsFromRevit()
      {
         bool hasRenamedBlocks = false;
         using (var t = _db.TransactionManager.StartTransaction())
         {
            var bt = t.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
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
                              _ed.WriteMessage("\nБлок {0} переименован в {1}", oldName, newName);
                              hasRenamedBlocks = true;
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
         if (!hasRenamedBlocks)
         {
            throw new Exception("Блоки квартир экспортированные из Revit не найдены.");
         }
      }

      // Подготовить задание
      public void Prepare()
      {
         Inspector.Clear();
         // переименование блоков квартир из ревитовских имен в нужные
         renameApartsFromRevit();
         // отключение слоев
         layerPrepare();
         // Создание блока осей
         createBlockAxles();
      }

      private void layerPrepare()
      {
         // Выключиь все слои, кроме Обобщенные модели и Сетки
         using (var t = _db.TransactionManager.StartTransaction())
         {
            var lt = t.GetObject(_db.LayerTableId, OpenMode.ForRead) as LayerTable;
            foreach (ObjectId idLayer in lt)
            {
               var lay = t.GetObject(idLayer, OpenMode.ForRead) as LayerTableRecord;
               if (_layersIncluded.Contains(lay.Name, StringComparer.CurrentCultureIgnoreCase))
               {
                  // включить и разморозить
                  if (lay.IsFrozen || lay.IsOff)
                  {
                     lay.UpgradeOpen();
                     if (lay.IsOff) lay.IsOff = false;
                     if (lay.IsFrozen) lay.IsFrozen = false;
                  }
               }
               else
               {
                  // включить и разморозить
                  if (!lay.IsOff)
                  {
                     lay.UpgradeOpen();
                     lay.IsOff = true; 
                  }
               }
            }
            t.Commit();
         }
      }

      private void createBlockAxles()
      {
         // запрос номера секции
         string sectionName = getSectionName("\nВведи имя секции");
         // запрос номера этажей
         string floorName = getSectionName("\nВведи номер этажа(ей)");
         // имя блока осей
         string blName = string.Format("АР_Оси_{0}_{1}", sectionName, floorName);
         if (checkBlock(blName))
         {
            var ids = selectFloor(string.Format("\nВыбор осей для создания блока {0}", blName));
            // запрос точки вставки
            Point3d location = getPoint(string.Format("Точка вставки блока осей {0}", blName)).TransformBy(_ed.CurrentUserCoordinateSystem);
            createBlock(ids, blName, location);
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
            newName = name.Substring(_prefixBlApart.Length, indexDash - _dash.Length).Trim();
            isApartBl = true;
         }
         return isApartBl;
      }

      // Запрос номера этажа
      private string getSectionName(string msg)
      {
         string sectionName = string.Empty;
         var prOpt = new PromptStringOptions(msg);                  
         do
         {
            var res = _ed.GetString(prOpt);
            if (res.Status == PromptStatus.OK)
            {
               sectionName = res.StringResult;               
            }
            else
            {
               throw new Exception("\nОтменено пользователем");
            }
         } while (string.IsNullOrWhiteSpace(sectionName));
         return sectionName;
      }

      private Point3d getPoint(string msg)
      {
         var res = _ed.GetPoint(msg);
         if (res.Status == PromptStatus.OK)
         {
            return res.Value;
         }
         else
         {
            throw new Exception("\nОтменено пользователем");
         }
      }

      // запрос выбора объектов этажа
      private List<ObjectId> selectFloor(string msg)
      {
         var selOpt = new PromptSelectionOptions();
         selOpt.MessageForAdding = msg;
         var selRes = _ed.GetSelection(selOpt);
         if (selRes.Status == PromptStatus.OK)
         {
            return selRes.Value.GetObjectIds().ToList();
         }
         else
         {
            throw new Exception("\nОтменено пользователем");
         }
      }

      private bool checkBlock(string blName)
      {
         try
         {
            SymbolUtilityServices.ValidateSymbolName(blName, false);
            using (var bt = _db.BlockTableId.Open(OpenMode.ForRead) as BlockTable)
            {
               return !bt.Has(blName);
            }
         }
         catch (Exception ex)
         {
            throw new Exception(string.Format("Недопустимое имя блока - {0}", blName), ex);
         }         
      }

      // создаение блока монтажки
      private void createBlock(List<ObjectId> idsEnt, string blName, Point3d location)
      {         
         using (var t = _db.TransactionManager.StartTransaction())
         {
            var bt = t.GetObject(_db.BlockTableId, OpenMode.ForWrite) as BlockTable;
            ObjectId idBtr;
            BlockTableRecord btr;
            // создание определения блока
            using (btr = new BlockTableRecord())
            {
               btr.Name = blName;
               idBtr = bt.Add(btr);
               t.AddNewlyCreatedDBObject(btr, true);
            }
            // копирование выбранных объектов в блок
            ObjectIdCollection ids = new ObjectIdCollection(idsEnt.ToArray());
            IdMapping mapping = new IdMapping();
            _db.DeepCloneObjects(ids, idBtr, mapping, false);

            // перемещение объектов в блоке
            btr = t.GetObject(idBtr, OpenMode.ForRead, false, true) as BlockTableRecord;
            var moveMatrix = Matrix3d.Displacement(Point3d.Origin - location);
            foreach (ObjectId idEnt in btr)
            {
               var ent = t.GetObject(idEnt, OpenMode.ForWrite, false, true) as Entity;
               ent.TransformBy(moveMatrix);
            }

            // удаление выбранных объектов
            foreach (ObjectId idEnt in ids)
            {
               var ent = t.GetObject(idEnt, OpenMode.ForWrite, false, true) as Entity;
               ent.Erase();
            }

            // вставка блока
            using (var blRef = new BlockReference(location, idBtr))
            {
               blRef.SetDatabaseDefaults(_db);
               var ms = t.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
               ms.AppendEntity(blRef);
               t.AddNewlyCreatedDBObject(blRef, true);
            }
            t.Commit();
         }
      }
   }
}
