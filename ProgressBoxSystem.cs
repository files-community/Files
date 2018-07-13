//  ---- ProgressBoxSystem.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains code for the progress indicator displayed for intensive item loading ---- 
//




using System.ComponentModel;
using Windows.UI.Xaml;

public class ProgressPercentage : INotifyPropertyChanged
{
    public int _prog;
    public int prog
    {
        get
        {
            return _prog;
        }

        set
        {
            if (value != _prog)
            {
                _prog = value;
                NotifyPropertyChanged("prog");
                //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI");
            }
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }
}

public class ProgressUIVisibility : INotifyPropertyChanged
{
    public Visibility _isVisible;
    public Visibility isVisible
    {
        get
        {
            return _isVisible;
        }

        set
        {
            if (value != _isVisible)
            {
                _isVisible = value;
                NotifyPropertyChanged("isVisible");
                //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
            }
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }
}

public class ProgressUIHeader : INotifyPropertyChanged
{
    public string _header;
    public string Header
    {
        get
        {
            return _header;
        }

        set
        {
            if (value != _header)
            {
                _header = value;
                NotifyPropertyChanged("Header");
                //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
            }
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }
}

public class ProgressUIPath : INotifyPropertyChanged
{
    public string _path;
    public string Path
    {
        get
        {
            return _path;
        }

        set
        {
            if (value != _path)
            {
                _path = value;
                NotifyPropertyChanged("Path");
                //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
            }
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string info)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }
}