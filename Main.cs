using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPIAddingParametersProject
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand

    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application; // создаем переменную, таким образом, как будто заходим в приложение Revit
            UIDocument uidoc = uiapp.ActiveUIDocument;// добираемся до свойств класса UIDocument
                                                      // обращаемся к uiapp и забираем ActiveUIDocument
                                                      // т.е интерфейс общедоступного документа
            Document doc = uidoc.Document; // обращаемся к своему документу, к базе данных его данных

            //в Revit есть определенные категории мы должны их определить
            var categorySet = new CategorySet();
            //указываем категории, добавляем параметры напр., для стен, дверей и окон
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Walls));
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Doors));
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_Windows));

            //поскольку будем производить запись в нашу модель для этого нам нужна транзакция
            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start(); // начало транзакции

                //создаем название метода в данном классе и определяем параметры которые будем туда передавать
                //(uiapp.Application с помощью Application сможем добраться до файла общих параметров;
                //далее передаем doc для того чтобы произвести добавление параметра; вводим название добавляемого параметра "Тест параметер";
                // сategorySet это для того, чтобы мы указали к каким именно категориям добавляется параметр;
                //BuiltInParameterGroup для того, чтобы определить группу в которую будет добавляться параметер напр., PG_DATA)
                CreateSharedParameter(uiapp.Application, doc, "TestParameter", categorySet, BuiltInParameterGroup.PG_DATA, true);

                ts.Commit();//конец транзакции, подтверждение изменений, которые производим в нашей моделе
            }

            return Result.Succeeded;
        }
                 //для создания метода CreateSharedParameter нажимаем Ctrl+точка, здесь нужно изменить некоторые названия
                 //напр., v1 это будет parameterName, pG_DATA меняем на  builtInParameterGroup,
                //v2 меняем на isInstance, а здесь мы определим является ли создаваемый параметр параметром экземпляра
                //Если передается true то параметр будет параметром экземпляра, если false, тогда это будет параметр типа 
        private void CreateSharedParameter(Application application, 
            Document doc, string parameterName, CategorySet categorySet, 
            BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            //мы добираемся до файлов общих параметров это возможно с помощью переменной типа Definition
            DefinitionFile definitionFile = application.OpenSharedParameterFile();

            //если definitionFile равен ничему
            if(definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");//выводим ошибку
                return; //возвращаем т.е выходим из данного метода
            }

            // из данного файла находим определение параметра для этого создаем переменную типа Definition
            //берем definitionFile, берем все группы, которые есть, выбираем от туда все определения параметров это SelectMany
            //таким образом мы собираем все определения параметров 
            //когда мы все определения параметров выбрали воспользуемся библиотекой Linkue выберет только нужный FirstOrDefault
            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));// с помощью метода FirstOrDefault выбрали только один параметр
                                                                       // (def => def.Name.Equals(parameterName) с заданым заранее именем
            //если параметр с именем parameterName не найден, тогда:
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;//и просто выходим из данного метода
            }

            //если все нормально мы нашли нужный параметр теперь его добавим
            //для этого создаем переменную типа binding обращаемся к application, вызываем метод Create и вызываем NewTypeBinding
            //и отправляем те категории которые определили выше в проекте categorySet
            Binding binding = application.Create.NewTypeBinding(categorySet);//это параметр типа

            //если у нас тот случай, когда создаем параметр экземпляра, то в этом случае указанную переменную мы заменяем на другой метод:
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);//это параметр экземпляра

            //далее воспользуемся переменной BindingMap для того чтобы вставить наш новый параметр т.е мы его создали, но пока не вставили
            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);//вставляем созданный параметр в чертеж

        }
    }
}
