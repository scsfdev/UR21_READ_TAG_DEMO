using System;
using System.Windows;
using System.Windows.Data;

namespace UR21_READ_TAG_DEMO.Model
{
    class Bol2VisColl : IValueConverter
    {
        public Bol2VisColl() { }

        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = (bool)value;

            if (bValue != Reverse)
            {
                return Visibility.Visible;
            }
            else
            {
                if (Collapse)
                    return Visibility.Collapsed;
                else
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return !Reverse;
            else
                return Reverse;
        }
    }

    class Value2Visible : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public bool bVisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = false;

            if (value != null && value.ToString().ToUpper() != "FALSE" && value.ToString().ToUpper() != "F" && value.ToString().ToUpper() != "0")
                bValue = true;

            if (bVisible)
            {
                if (bValue != Reverse)
                {
                    return Visibility.Visible;
                }
                else
                {
                    if (Collapse)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Hidden;
                }
            }
            else
            {
                if (bValue != Reverse)
                    return true;
                else
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (bVisible)
            {
                Visibility visibility = (Visibility)value;

                if (visibility == Visibility.Visible)
                    return !Reverse;
                else
                    return Reverse;
            }
            else
            {
                bool bValue = (bool)value;
                return Reverse & bValue;
            }

        }
    }

    class Obj2Enable : IValueConverter
    {
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value == null ? "0" : value.ToString().Trim().ToUpper();

            bool bReturn = false;

            switch (strValue)
            {
                case "1":
                case "TRUE":
                case "T":
                    bReturn = true;
                    break;
                case "FALSE":
                case "F":
                case "":
                case "0":
                    bReturn = false;
                    break;
                default:
                    bReturn = false;
                    break;
            }

            if (Reverse)
                return !bReturn;
            else
                return bReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "1";
                else
                    return "0";
            }
            return "0";
        }
    }
}
