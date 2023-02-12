using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DCS_Radio_Presets;

public partial class MainWindow : Window
{
    private DCS dcs;
    private CommPlan plan;
    private Mission? mission;

    public MainWindow()
    {
        Resources.Add("modulations", new[] { "AM", "FM" });

        InitializeComponent();

        dcs = new DCS();
        if(!string.IsNullOrWhiteSpace(Properties.Settings.Default.DcsPath))
            DcsPath.Text = Properties.Settings.Default.DcsPath;
        else
            LoadDcsData();
        
        MissionsPath.Text = Properties.Settings.Default.MissionsDefaultPath;
        CreateKneeboardFiles.IsChecked = Properties.Settings.Default.CreateKneeboardFiles;
        
        if (PlanTab.IsEnabled)
            PlanTab.IsSelected = true;

        plan = new CommPlan();
        Comms.ItemsSource = plan.Frequencies;
        Templates.ItemsSource = plan.Templates;
        Templates2.ItemsSource = plan.Templates;
        Templates2.SelectedIndex = plan.Templates.Any() ? 0 : -1;
    }

    private void AddTheatreFrequenciesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var theatre = dcs.LoadTheatre(DcsPath.Text, dcs.Theatres[Theatre.SelectedIndex]);
        plan.AddTheatreFrequencies(theatre);
    }

    private void SavePlanButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            OverwritePrompt = true,
            Filter = "Comm plan files (*.plan)|*.plan|All files (*.*)|*.*",
            AddExtension = true
        };
        if (dialog.ShowDialog() != true)
            return;

        plan.Save(dialog.FileName);
    }

    private void AddTemplateButton_OnClick(object sender, RoutedEventArgs e)
    {
        var aircraft = (AircraftModel)AircraftTemplate.SelectedItem;

        plan.Templates.Add(new Template
        {
            Type = aircraft.Type,
            Channels = new ObservableCollection<TemplateChannel>(
                aircraft.Channels.Select(x => new TemplateChannel(x, plan.Frequencies)))
        });
        if (Templates2.Items.Count == 1)
            Templates2.SelectedIndex = 0;
    }

    private void Templates_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Templates.SelectedIndex < 0 || Templates.SelectedIndex >= plan.Templates.Count)
        {
            Radios.ItemsSource = null;
            CopyTemplate.IsEnabled = false;
            DeleteTemplate.IsEnabled = false;
            return;
        }

        CopyTemplate.IsEnabled = true;
        DeleteTemplate.IsEnabled = true;
        Radios.ItemsSource = plan.Templates[Templates.SelectedIndex].Channels;
    }

    private void DeleteTemplateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Templates.SelectedIndex < 0 || Templates.SelectedIndex >= plan.Templates.Count)
            return;

        var result = MessageBox.Show("Are you sure you want to delete template " +
                                     plan.Templates[Templates.SelectedIndex].Type + " - " +
                                     plan.Templates[Templates.SelectedIndex].Name + "?", "Delete confirmation",
            MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes)
        {
            plan.Templates.RemoveAt(Templates.SelectedIndex);
        }
    }

    private void OpenPlanButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Comm plan files (*.plan)|*.plan|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true)
            return;

        plan = CommPlan.Load(dialog.FileName, dcs.Aircrafts);
        
        Comms.ItemsSource = plan.Frequencies;
        Templates.ItemsSource = plan.Templates;
        Templates2.ItemsSource = plan.Templates;
        Templates2.SelectedIndex = plan.Templates.Any() ? 0 : -1;
    }

    private void NewPlanButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (plan.Frequencies.Any() || plan.Templates.Any())
        {
            var result = MessageBox.Show("Discard existing plan and create a new plan?", "Confirmation",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;
        }
        
        plan = new CommPlan();
        Comms.ItemsSource = plan.Frequencies;
        Templates.ItemsSource = plan.Templates;
        Templates2.ItemsSource = plan.Templates;
        Templates2.SelectedIndex = -1;
    }

    private void OpenMission_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Mission files (*.miz)|*.miz|All files (*.*)|*.*",
            InitialDirectory = MissionsPath.Text
        };
        if (dialog.ShowDialog() == true)
        {
            LoadMission(dialog.FileName);
            MissionTab.IsSelected = true;
            Groups.ItemsSource = mission.Groups;
            Units.ItemsSource = null;
            Presets.ItemsSource = null;
        }
    }

    private void LoadMission(string filePath)
    {
        mission = Mission.Load(filePath, dcs.Aircrafts);
    }

    private void Groups_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Groups.SelectedIndex < 0 || Groups.SelectedIndex > mission.Groups.Count)
        {
            Units.ItemsSource = null;
            Presets.ItemsSource = null;
            return;
        }

        Units.ItemsSource = mission.Groups[Groups.SelectedIndex].Units;
        Units.SelectedIndex = Units.Items.Count > 0 ? 0 : -1;
    }

    private void Units_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Units.SelectedIndex < 0 || Units.SelectedIndex > mission.Groups[Groups.SelectedIndex].Units.Count)
        {
            Presets.ItemsSource = null;
            return;
        }

        Presets.ItemsSource = mission.Groups[Groups.SelectedIndex].Units[Units.SelectedIndex].Presets;
    }

    private void SaveMission_OnClick(object sender, RoutedEventArgs e)
    {
        if (mission == null)
            return;
        
        var dialog = new SaveFileDialog
        {
            Filter = "Mission files (*.miz)|*.miz|All files (*.*)|*.*",
            InitialDirectory = MissionsPath.Text,
            OverwritePrompt = true,
            AddExtension = true
        };
        if (dialog.ShowDialog() != true)
            return;
        
        mission.Save(dialog.FileName, CreateKneeboardFiles.IsChecked == true);
    }

    private void ApplyMatchingTemplates_OnClick(object sender, RoutedEventArgs e)
    {
        if (mission == null)
            return;
        
        var duplicates = mission.ApplyMatchingTemplates(plan).ToArray();
        Presets.Items.Refresh();
        if (duplicates.Any())
            MessageBox.Show("Multiple templates found for " + string.Join(", ", duplicates) +
                            ". Templates were applied only for unit types with one matching template.");
    }

    private bool isEditing;
    private bool isDragging;
    private Preset? draggedPreset;
    private PlanFrequency[] draggedPlanFrequencies = Array.Empty<PlanFrequency>();
        
    private void ResetDragDrop()
    {
        isDragging = false;
        Popup1.IsOpen = false;
        Presets.IsReadOnly = false;
        Comms.IsReadOnly = false;
    }

    private void OnBeginEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        isEditing = true;
        if (isDragging) ResetDragDrop();
    }

    private void OnEndEdit(object sender, DataGridCellEditEndingEventArgs e)
    {
        isEditing = false;
    }
        
    private void Presets_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (isEditing) return;

        var row = UiHelpers.TryFindFromPoint<DataGridRow>((UIElement) sender, e.GetPosition(Presets));
        if (row == null) return;
        
        if(UiHelpers.TryFindFromPoint<ComboBox>((UIElement) sender, e.GetPosition(Presets)) != null)
            return;

        isDragging = true;
        draggedPreset = (Preset) row.Item;
    }
        
    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDragging || isEditing)
            return;

        if (draggedPreset != null)
        {
            var row = UiHelpers.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(DockPanel));
            if (row?.Item is Preset targetItem && !ReferenceEquals(draggedPreset, targetItem))
            {
                targetItem.Swap(draggedPreset);
                Presets.Items.Refresh();
            }
        }

        if (draggedPlanFrequencies.Length > 0)
        {
            var row = UiHelpers.TryFindFromPoint<DataGridRow>((UIElement) sender, e.GetPosition(DockPanel));
            if (row?.Item is TemplateChannel targetChannel)
            {
                var sourceIndexes = draggedPlanFrequencies.Select(x => Comms.Items.IndexOf(x)).OrderBy(x => x).ToArray();
                var index = Radios.Items.IndexOf(targetChannel);
                for (var i = 0; i < sourceIndexes.Length && i + index < Radios.Items.Count; ++i)
                {
                    var label = (Comms.Items[sourceIndexes[i]] as PlanFrequency)!.Label;
                    if (!string.IsNullOrWhiteSpace(label))
                        (Radios.Items[i + index] as TemplateChannel)!.Label = Regex.Replace(label, "G\\d+", "G#");
                }
                Radios.Items.Refresh();
            }

            if (row?.Item is PlanFrequency targetFrequency && draggedPlanFrequencies.Length == 1 && !ReferenceEquals(draggedPlanFrequencies[0], targetFrequency))
            {
                targetFrequency.Swap(draggedPlanFrequencies[0]);
                Comms.Items.Refresh();
            }
        }
        
        ResetDragDrop();
    }
        
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!isDragging || e.LeftButton != MouseButtonState.Pressed) return;

        if (!Popup1.IsOpen)
        {
            Presets.IsReadOnly = true;
            Comms.IsReadOnly = true;
            Popup1.IsOpen = true;
        }

        var popupSize = new Size(Popup1.ActualWidth, Popup1.ActualHeight);
        Popup1.PlacementRectangle = new Rect(e.GetPosition(this), popupSize);
    }

    private void BrowseDcsPath_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            DcsPath.Text = dialog.FileName;
        }
    }

    private void BrowseMissionsPath_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            MissionsPath.Text = dialog.FileName;
        }
    }

    private void LoadDcsData()
    {
        var directoryOk = dcs.Load(DcsPath.Text);
        DcsFolderPrompt.Visibility = directoryOk ? Visibility.Hidden : Visibility.Visible;
        
        Theatre.ItemsSource = dcs.Theatres;
        AircraftTemplate.ItemsSource = dcs.Aircrafts;
            
        Theatre.SelectedIndex = dcs.Theatres.Any() ? 0 : -1;
        AddTheatreFrequenciesButton.IsEnabled = dcs.Theatres.Any();
        
        AircraftTemplate.SelectedIndex = dcs.Aircrafts.Any() ? 0 : -1;
    }

    private void DcsPath_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        LoadDcsData();
        Properties.Settings.Default.DcsPath = DcsPath.Text;
        Properties.Settings.Default.Save();
    }

    private void MissionsDefaultPath_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Properties.Settings.Default.MissionsDefaultPath = MissionsPath.Text;
        Properties.Settings.Default.Save();
    }

    private void CreateKneeboardFiles_Changed(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.CreateKneeboardFiles = CreateKneeboardFiles.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    private void ApplyToGroupButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Groups.SelectedItem == null)
            return;
        if(Templates2.SelectedItem == null)
            return;

        var template = (Template)Templates2.SelectedItem;
        var group = (UnitGroup)Groups.SelectedItem;

        group.ApplyTemplate(template, plan.Frequencies);
        
        Presets.Items.Refresh();
    }

    private void ApplyToUnitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Units.SelectedItem == null)
            return;
        if(Groups.SelectedItem == null)
            return;
        if(Templates2.SelectedItem == null)
            return;

        var template = (Template)Templates2.SelectedItem;
        var unit = (Unit)Units.SelectedItem;
        unit.ApplyTemplate(template, plan.Frequencies);
        Presets.Items.Refresh();
    }

    private void ResetGroupButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Groups.SelectedItem == null)
            return;
        foreach (var unit in ((UnitGroup)Groups.SelectedItem).Units)
        {
            foreach (var preset in unit.Presets)
            {
                preset.Reset();
            }
        }
        Presets.Items.Refresh();
    }

    private void ResetUnitbutton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Units.SelectedItem == null)
            return;
        foreach (var preset in ((Unit)Units.SelectedItem).Presets)
        {
            preset.Reset();
        }
        Presets.Items.Refresh();
    }

    private void Comms_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (isEditing) return;

        var row = UiHelpers.TryFindFromPoint<DataGridRow>((UIElement) sender, e.GetPosition(Comms));
        if (row is not { Item: PlanFrequency item }) return;

        if(UiHelpers.TryFindFromPoint<ComboBox>((UIElement) sender, e.GetPosition(Comms)) != null)
            return;
        
        isDragging = true;
        
        if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
        {
            var last = Comms.SelectedItems[^1]!;
            var start = Comms.Items.IndexOf(last);
            var end = Comms.Items.IndexOf(item);
            var items = new HashSet<PlanFrequency>(Comms.SelectedItems.Cast<PlanFrequency>());
            for (var i = Math.Min(start, end); i <= Math.Max(start, end); ++i)
            {
                items.Add((PlanFrequency)Comms.Items[i]);
            }

            draggedPlanFrequencies = items.ToArray();
        }
        else if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
        {
            draggedPlanFrequencies = Comms.SelectedItems.Cast<PlanFrequency>().Concat(new[] { item })
                .ToArray();
        }
        else
        {
            draggedPlanFrequencies = new[] { item };
        }
    }

    private void Comms_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        (sender as DataGrid)!.RowEditEnding -= Comms_OnRowEditEnding;
        (sender as DataGrid)!.CommitEdit();
        (sender as DataGrid)!.Items.Refresh();
        (sender as DataGrid)!.RowEditEnding += Comms_OnRowEditEnding;
        
        var changed = false;
        foreach (TemplateChannel channel in Radios.Items)
            changed |= channel.CheckRanges();
        if(changed)
            Radios.Items.Refresh();
        
        //TODO: Should frequency changes in plan be automatically applied to labeled presets? 
    }

    private void Comms_ModulationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var changed = false;
        foreach (TemplateChannel channel in Radios.Items)
            changed |= channel.CheckRanges();
        if(changed)
            Radios.Items.Refresh();
    }

    private void CopyTemplate_OnClick(object sender, RoutedEventArgs e)
    {
        if (Templates.SelectedItem is not Template template)
            return;
        plan.Templates.Add(new Template(template));
    }

    private void AircraftTemplate_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AddTemplate.IsEnabled = AircraftTemplate.SelectedItem != null;
    }

    private void CopyPresetsToGroup_OnClick(object sender, RoutedEventArgs e)
    {
        if (mission == null)
            return;
        if (Units.SelectedItem is not Unit unit)
            return;
        if (Groups.SelectedItem is not UnitGroup group)
            return;
        
        group.ApplyUnit(unit);
    }

    private void Presets_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        (sender as DataGrid)!.RowEditEnding -= Presets_OnRowEditEnding;
        (sender as DataGrid)!.CommitEdit();
        //(sender as DataGrid)!.Items.Refresh();
        (sender as DataGrid)!.RowEditEnding += Presets_OnRowEditEnding;

        if (e.Row.Item is not Preset preset)
            return;

        var freq = plan.Frequencies.FirstOrDefault(x => x.Label == preset.Label);
        if (freq == null)
            return;

        if(preset.Apply(freq, preset.Label))
            Presets.Items.Refresh();
    }
}