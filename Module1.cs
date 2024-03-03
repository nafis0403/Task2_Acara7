using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LayerSymbology
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current
        {
            get
            {
                return _this ?? (_this = (Module1)FrameworkApplication.FindModule("LayerSymbology_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides

        #region Business Logic

        public static void ApplySymbol(string symbolChoice)
        {
            QueuedTask.Run(() =>
            {
                // Check for an active 2D mapview, if not, then prompt and exit.
                if (MapView.Active == null || (MapView.Active.ViewingMode != ArcGIS.Core.CIM.MapViewingMode.Map))
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("An active 2D MapView is required to use this tool. Exiting...", "Info");
                    return;
                }
                // Get the layer(s) selected in the Contents pane, if there is not just one, then prompt then exit.
                if (MapView.Active.GetSelectedLayers().Count != 1)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("One feature layer must be selected in the Contents pane. Exiting...", "Info");
                    return;
                }
                // Check to see if the selected layer is a feature layer, if not, then prompt and exit.
                var featLayer = MapView.Active.GetSelectedLayers().First() as FeatureLayer;
                if (featLayer == null)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("A feature layer must be selected in the Contents pane. Exiting...", "Info");
                    return;
                }
                // Check if the feature layer shape type is point, if not, then prompt and exit.
                else if (featLayer.ShapeType != esriGeometryType.esriGeometryPoint)
                {
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Selected feature layer must be shape type POINT. Exiting...", "Info");
                    return;
                }
                try
                {
                    // If symbolChoice is a single symbol
                    if (symbolChoice == "single symbol")
                    {
                        // Get all styles in the project
                        var styles = Project.Current.GetItems<StyleProjectItem>();

                        // Get a specific style in the project
                        StyleProjectItem style = styles.First(s => s.Name == "ArcGIS 2D");

                        // Get the Push Pin 1 symbol
                        var pt_ssi = style.SearchSymbols(StyleItemType.PointSymbol, "Push Pin 1").FirstOrDefault();

                        // Create a new renderer definition and reference the symbol
                        SimpleRendererDefinition srDef = new SimpleRendererDefinition();
                        srDef.SymbolTemplate = pt_ssi.Symbol.MakeSymbolReference();

                        // Create the renderer and apply the definition
                        CIMSimpleRenderer ssRenderer = (CIMSimpleRenderer)featLayer.CreateRenderer(srDef);

                        // Update the feature layer renderer
                        featLayer.SetRenderer(ssRenderer);
                    }
                    else if (symbolChoice == "graduated color")
                    {
                        // Get a style and color ramp to apply to the renderer
                        StyleProjectItem style = Project.Current.GetItems<StyleProjectItem>().First(s => s.Name == "ColorBrewer Schemes (RGB)");
                        IList<ColorRampStyleItem> colorRampList = style.SearchColorRamps("Greens (Continuous)");
                        ColorRampStyleItem aColorRamp = colorRampList[0];

                        // Create a graduated color renderer definition with 3 breaks, a populated numeric field named "Symbol" is required
                        GraduatedColorsRendererDefinition gcDef = new GraduatedColorsRendererDefinition()
                        {
                            ClassificationField = "Symbol",
                            ClassificationMethod = ArcGIS.Core.CIM.ClassificationMethod.EqualInterval,
                            BreakCount = 4,
                            ColorRamp = aColorRamp.ColorRamp,
                            SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol().MakeSymbolReference(),
                        };

                        // Create the renderer and apply the definition
                        CIMClassBreaksRenderer cbRndr = (CIMClassBreaksRenderer)featLayer.CreateRenderer(gcDef);

                        // Update the feature layer renderer
                        featLayer.SetRenderer(cbRndr);
                    }
                }
                catch (Exception exc)
                {
                    // Catch any exception found and display in a message box
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Exception caught: " + exc.Message);
                    return;
                }
            });
        }

        #endregion
    }
}
