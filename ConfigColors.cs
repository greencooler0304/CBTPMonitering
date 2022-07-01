using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;

namespace CBTPTempMoistMoni.Control
{
    public class ConfigColors
    {
        public static Color TitleBack = Color.FromRgb(78, 78, 80);

        public static Color Back = Color.FromRgb(45, 45, 48);

        public static Color Back_0 = Color.FromRgb(78, 78, 80); //  <-- title Back
        public static Color Back_1 = Color.FromRgb(67, 67, 70);
        public static Color Back_2 = Color.FromRgb(63, 63, 70); //  <-- button back
        public static Color Back_3 = Color.FromRgb(51, 51, 55); //  <-- panel back
        public static Color Back_4 = Color.FromRgb(45, 45, 48); //  <-- normal back
        public static Color Back_5 = Color.FromRgb(30, 30, 30);
        public static Color Back_6 = Color.FromRgb(10, 10, 10); //


        public static Color Label_0 = Color.FromRgb(255, 255, 255);
        public static Color Label_1 = Color.FromRgb(241, 241, 241);
        public static Color Label_2 = Color.FromRgb(165, 165, 165);
        public static Color Label_3 = Color.FromRgb(153, 153, 153);

        public static Color Label_disable = Color.FromRgb(153, 153, 153);

        public static Color Text_Reserved = Color.FromRgb(153, 153, 153);
        public static Color Text_ClassName = Color.FromRgb(78, 201, 176);
        public static Color Text_Comment = Color.FromRgb(87, 166, 74);
        public static Color Text_Number = Color.FromRgb(181, 206, 168);
        public static Color Text_LineNumber = Color.FromRgb(43, 145, 175);
        public static Color Text_String = Color.FromRgb(214, 157, 133);


        public static Color ToolBarButton_0 = Color.FromRgb(199, 199, 199);
        public static Color ToolBarButton_1 = Color.FromRgb(240, 202, 148);
        public static Color ToolBarButton_2 = Color.FromRgb(215, 172, 106);
        public static Color ToolBarButton_3 = Color.FromRgb(122, 193, 255);
        public static Color ToolBarButton_4 = Color.FromRgb(255, 119, 74);
        public static Color ToolBarButton_5 = Color.FromRgb(200, 50, 0);

        public static Color TabSelected = Color.FromRgb(0, 122, 204);
        public static Color TabUnSelected = Color.FromRgb(63, 63, 70);

        public static Color Button_Start = Color.FromRgb(173, 216, 230);
        public static Color Button_Stop = Color.FromRgb(255, 182, 193);
        public static Color Button_ForcedStop = Color.FromRgb(200, 50, 0);
        public static Color Button_Disable = Color.FromRgb(78, 78, 80);

        public static Color SubTitleBack = Color.FromRgb(144, 238, 144);


        public static Color Blue_0 = Color.FromRgb(21, 74, 129);    //  sub title
        public static Color Blue_1 = Color.FromRgb(0, 122, 204);
        public static Color Blue_2 = Color.FromRgb(86, 156, 214);
        //public static Color Blue_2 = Color.FromRgb(214, 157, 133);
        //public static Color Blue_3 = Color.FromRgb(214, 157, 133);
        public static Color Border_Pink = Color.FromRgb(0xD2, 0x94, 0xE2);
        public static Color Border_Green = Color.FromRgb(0x88, 0xD1, 0xA8);


        [XmlIgnore] public Color ColorBack { get { return ConfigColors.Back_4; } }
        [XmlIgnore] public Color ColorPanelBack { get { return ConfigColors.Back_3; } }
        [XmlIgnore] public Color ColorButtonBack { get { return ConfigColors.Back_2; } }

        [XmlIgnore] public Color ColorTitleBack { get { return ConfigColors.Blue_2; } }
        [XmlIgnore] public Color ColorSubTitleBack { get { return ConfigColors.Back_0; } }

        [XmlIgnore] public Color ColorBtnStartBack { get { return ConfigColors.Button_Start; } }
        [XmlIgnore] public Color ColorBtnStopBack { get { return ConfigColors.Button_Stop; } }
        [XmlIgnore] public Color ColorBtnForcedStopBack { get { return ConfigColors.Button_ForcedStop; } }
        [XmlIgnore] public Color ColorBtnDisable { get { return ConfigColors.Label_3; } }
        [XmlIgnore] public Color ColorBtnDisableBack { get { return ConfigColors.Button_Disable; } }

        [XmlIgnore] public Color ColorDut { get { return ConfigColors.ToolBarButton_2; } }

        [XmlIgnore] public Color ColorTitle { get { return ConfigColors.Label_1; } }
        [XmlIgnore] public Color ColorLabel { get { return ConfigColors.Label_1; } }
        [XmlIgnore] public Color ColorBorderLabel { get { return ConfigColors.Text_LineNumber; } }

        [XmlIgnore] public Color ColorAccentEditBack { get { return ConfigColors.Back_2; } }
        [XmlIgnore] public Color ColorEditBack { get { return ConfigColors.Back_5; } }
        [XmlIgnore] public Color ColorEditErrorBack { get { return ConfigColors.ToolBarButton_4; } }

        [XmlIgnore] public Color ColorTime { get { return ConfigColors.Text_ClassName; } }
        [XmlIgnore] public Color ColorLog { get { return ConfigColors.Text_Comment; } }
        [XmlIgnore] public Color ColorAccentLog { get { return ConfigColors.Text_Number; } }

        [XmlIgnore] public Color ColorTabSelected { get { return ConfigColors.TabSelected; } }
        [XmlIgnore] public Color ColorTabUnSelect { get { return ConfigColors.TabUnSelected; } }

        [XmlIgnore] public Color ColorPortOpened { get { return ConfigColors.Text_ClassName; } }
    }
}
