﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;

namespace Mindscape.Raygun4Net
{
  public class RaygunHttpModule : IHttpModule
  {
    private bool ExcludeErrorsBasedOnHttpStatusCode { get; set; }
    private bool ExcludeErrorsFromLocal { get; set; }

    private int[] HttpStatusCodesToExclude { get; set; }

    public void Init(HttpApplication context)
    {
      context.Error += SendError;

      HttpStatusCodesToExclude = new int[0];
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.ExcludeHttpStatusCodesList))
      {
        List<int> codes = new List<int>();
        string[] parts = RaygunSettings.Settings.ExcludeHttpStatusCodesList.Split(',');
        foreach(string code in parts)
        {
          int c;
          if (int.TryParse(code, out c))
          {
            codes.Add(c);
          }
        }
        HttpStatusCodesToExclude = codes.ToArray();
      }
      ExcludeErrorsBasedOnHttpStatusCode = HttpStatusCodesToExclude.Length > 0;
      ExcludeErrorsFromLocal = RaygunSettings.Settings.ExcludeErrorsFromLocal;
    }

    public void Dispose()
    {
    }

    private void SendError(object sender, EventArgs e)
    {
      var application = (HttpApplication)sender;
      var lastError = application.Server.GetLastError();

      if (CanSend(lastError))
      {
        var client = GetRaygunClient(application);
        client.SendInBackground(lastError);
      }
    }

    protected RaygunClient GetRaygunClient(HttpApplication application)
    {
      var raygunApplication = application as IRaygunApplication;
      return raygunApplication != null ? raygunApplication.GenerateRaygunClient() : new RaygunClient();
    }

    protected bool CanSend(Exception exception)
    {
      if (ExcludeErrorsBasedOnHttpStatusCode && exception is HttpException && Contains(HttpStatusCodesToExclude, ((HttpException)exception).GetHttpCode()))
      {
        return false;
      }

      if (ExcludeErrorsFromLocal && HttpContext.Current.Request.IsLocal)
      {
        return false;
      }

      return true;
    }

    private static bool Contains(int[] array, int target)
    {
      foreach (int i in array)
      {
        if (i.Equals(target))
        {
          return true;
        }
      }
      return false;
    }
  }
}
