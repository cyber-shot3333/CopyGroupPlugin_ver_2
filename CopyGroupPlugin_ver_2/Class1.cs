using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin_ver_2
{
    [TransactionAttribute(TransactionMode.Manual)]


    public class CopyGroupCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter(); 		// экземпляр класса фильтрации
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу для копирования");
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                XYZ groupCenter = GetElementCenter(group);		// точка центра группы
                Room room = GetRoomByPoint(doc, groupCenter);	// комната, в которой находится выбранная группа
                XYZ roomCenter = GetElementCenter(room);		// точка центра комнаты
                XYZ offset = groupCenter - roomCenter;			// смещение центра группы относительно центра комнаты

                XYZ point = uidoc.Selection.PickPoint("Выберите точку для вставки");
                Room selectedRoom = GetRoomByPoint(doc, point); 		// определяем какой комнате принадлежит точка, указанная пользователем
                XYZ selectedRoomCenter = GetElementCenter(selectedRoom); // определяем центр это комнаты
                XYZ pastePoint = selectedRoomCenter + offset; 	// определяем точку вставки группы в указанную комнату

                Transaction ts = new Transaction(doc);
                ts.Start("Идет копирование группы объектов");
                doc.Create.PlaceGroup(pastePoint, group.GroupType);
                ts.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)  // обработка исключения кнопки отмены (Esc)
            {
                return Result.Cancelled;			// завершается работа приложения, с результатом отмены
            }
            catch (Exception ex)
            {
                message = ex.Message;			// текст ошибки, вызванной исключением
                return Result.Failed;			// завершается работа приложения, с результатом ошибки	
            }

            return Result.Succeeded;
        }
        public XYZ GetElementCenter(Element element)		// метод определения центра объекта
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;			// возвращаем значение центра
        }

        public Room GetRoomByPoint(Document doc, XYZ point) 		// метод определяет по точке, какая это комната
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);    // фильтр
            collector.OfCategory(BuiltInCategory.OST_Rooms);				// отбор только комнат
            foreach (Element e in collector)
            {
                Room room = e as Autodesk.Revit.DB.Architecture.Room;		// если преобразование не удалось,  то room = null
                if (room != null)
                {
                    if (room.IsPointInRoom(point))		// проверяем, принадлежит ли точка данной комнате 
                    {
                        return room;
                    }
                }
            }
            return null;
        }

        public class GroupPickFilter : ISelectionFilter				//класс для фильтрации объектов
        {
            public bool AllowElement(Element elem)
            {
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)  // проверяем, что Id  объекта, над которым находится курсор =Id  группы (IOSModelGroups)
                { return true; }
                else return false;
            }
            public bool AllowReference(Reference reference, XYZ position)  	//для ссылок всегда будет false
            {
                return false;
            }
        }
    }

}
