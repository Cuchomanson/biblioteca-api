using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class ConvencionAgrupaPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Ejemplo: "Controllers.V1
            var nameSpaceController = controller.ControllerType.Namespace;
            var version = nameSpaceController?.Split('.').Last().ToLower();

            controller.ApiExplorer.GroupName = version;
        }
    }
}
