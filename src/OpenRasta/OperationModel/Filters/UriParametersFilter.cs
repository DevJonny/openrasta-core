﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using OpenRasta.Binding;
using OpenRasta.Collections;
using OpenRasta.Diagnostics;
using OpenRasta.Pipeline;
using OpenRasta.TypeSystem.ReflectionBased;
using OpenRasta.Web;

namespace OpenRasta.OperationModel.Filters
{
  public class UriParametersFilter : IOperationFilter
  {
    readonly PipelineData _pipelineData;

    public UriParametersFilter(ICommunicationContext context, IErrorCollector collector)
    {
      Logger = NullLogger.Instance;
      Errors = collector;
      _pipelineData = context.PipelineData;
    }

    IErrorCollector Errors { get; set; }
    ILogger Logger { get; set; }

    public IEnumerable<IOperationAsync> Process(IEnumerable<IOperationAsync> operations)
    {
      int acceptedMethods = 0;

      var selectedResourceUriTemplateParameters = _pipelineData.SelectedResource.UriTemplateParameters.ToList();
      foreach (var operation in operations)
      {
        if (IsEmpty(selectedResourceUriTemplateParameters))
        {
          LogAcceptNoUriParameters(operation);
          acceptedMethods++;
          yield return operation;
          continue;
        }

        foreach (var uriParameterMatches in selectedResourceUriTemplateParameters)
        {
          var uriParametersCopy = new NameValueCollection(uriParameterMatches);

          var matchedParameters = (
            from member in operation.Inputs
            from matchedParameterName in uriParametersCopy.AllKeys
            where TrySetPropertyAndRemoveUsedKey(member, matchedParameterName, uriParametersCopy, ConvertFromString)
            select matchedParameterName).ToList();

          if (matchedParameters.Count != uriParameterMatches.Count) continue;
          
          LogOperationAccepted(uriParameterMatches, operation);
          acceptedMethods++;
          yield return operation;
        }
      }

      LogAcceptedCount(acceptedMethods);

      if (acceptedMethods <= 0)
        Errors.AddServerError(CreateErrorNoOperationFound(selectedResourceUriTemplateParameters));
    }

    static BindingResult ConvertFromString(string strings, Type entityType)
    {
      try
      {
        return BindingResult.Success(entityType.CreateInstanceFrom(strings));
      }
      catch (Exception e)
      {
        if (e.InnerException is FormatException)
          return BindingResult.Failure();
        throw;
      }
    }

    static ErrorFrom<UriParametersFilter> CreateErrorNoOperationFound(
      IEnumerable<NameValueCollection> uriTemplateParameters)
    {
      return new ErrorFrom<UriParametersFilter>
      {
        Title = "No method matched the uri parameters",
        Message =
          $"None of the operations had members that could be matches against the uri parameters:\r\n{FormatUriParameterMatches(uriTemplateParameters)}"
      };
    }

    static string FormatUriParameterMatches(IEnumerable<NameValueCollection> uriParameterMatches)
    {
      var builder = new StringBuilder();
      foreach (var nvc in uriParameterMatches)
        builder.AppendLine(nvc.ToHtmlFormEncoding());
      return builder.ToString();
    }

    static bool IsEmpty(IList<NameValueCollection> parameters)
    {
      return parameters.Count == 0 || (parameters.Count == 1 && parameters[0].Count == 0);
    }

    void LogAcceptedCount(int operationCount)
    {
      Logger.WriteInfo("Found {0} potential operations to resolve.", operationCount);
    }

    void LogAcceptNoUriParameters(IOperationAsync operation)
    {
      Logger.WriteDebug("Empty parameter list, selected method {0}.", operation);
    }

    void LogOperationAccepted(NameValueCollection uriParameterMatches, IOperationAsync operation)
    {
      Logger.WriteDebug(
        "Accepted operation {0} with {1} matched parameters for uri parameter list {2}.",
        operation.Name,
        operation.Inputs.CountReady(),
        uriParameterMatches.ToHtmlFormEncoding());
    }

    bool TrySetPropertyAndRemoveUsedKey(InputMember member, string uriParameterName, NameValueCollection uriParameters,
      ValueConverter<string> converter)
    {
      if (member.Binder.SetProperty(uriParameterName, uriParameters.GetValues(uriParameterName), converter))
      {
        uriParameters.Remove(uriParameterName);
        return true;
      }

      return false;
    }
  }
}