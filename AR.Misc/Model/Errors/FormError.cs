﻿using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace AR.Misc.Model.Errors
{
   public partial class FormError : Form
   {
      private Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
      private BindingSource _binding;

      public FormError()
      {
         InitializeComponent();

         _binding = new BindingSource();
         _binding.DataSource = Inspector.Errors;            
         listBoxError.DataSource = _binding;
         listBoxError.DisplayMember = "ShortMsg";         
         textBoxErr.DataBindings.Add("Text", _binding , "Message", false, DataSourceUpdateMode.OnPropertyChanged);                      
      }

      private void buttonShow_Click(object sender, EventArgs e)
      {
         Error err = listBoxError.SelectedItem as Error;
         if (err != null && err.HasEntity)
         {            
            ed.Zoom(err.Extents);
         }
      }

      private void listBoxError_DoubleClick(object sender, EventArgs e)
      {
         buttonShow_Click(null, null);
      }

      private void listBoxError_SelectedIndexChanged(object sender, EventArgs e)
      {
         Error err = listBoxError.SelectedItem as Error;
         if (err != null)
         {
            buttonShow.Enabled = err.HasEntity; 
         }
      }
   }
}
