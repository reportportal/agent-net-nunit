using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.NUnitExtension.EventArguments;
using ReportPortal.Shared;
using System;
using System.Collections.Generic;
using System.Xml;

namespace ReportPortal.NUnitExtension
{
    public partial class ReportPortalListener
    {
        public delegate void SuiteStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event SuiteStartedHandler BeforeSuiteStarted;
        public static event SuiteStartedHandler AfterSuiteStarted;

        private void StartSuite(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId").Value;
                var name = xmlDoc.SelectSingleNode("/*/@name").Value;

                var startSuiteRequest = new StartTestItemRequest
                {
                    LaunchId = Bridge.Context.LaunchId,
                    StartTime = DateTime.UtcNow,
                    Name = name,
                    Type = TestItemType.Suite
                };

                var beforeSuiteEventArg = new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest);
                try
                {
                    if (BeforeSuiteStarted != null) BeforeSuiteStarted(this, beforeSuiteEventArg);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Exception was thrown in 'BeforeSuiteStarted' subscriber." + Environment.NewLine + exp);
                }
                if (!beforeSuiteEventArg.Canceled)
                {
                    TestItem test;
                    if (string.IsNullOrEmpty(parentId))
                    {
                        test = Bridge.Service.StartTestItem(startSuiteRequest);
                    }
                    else
                    {
                        test = Bridge.Service.StartTestItem(_suitesFlow[parentId].Id, startSuiteRequest);
                    }

                    _suitesFlow[id] = beforeSuiteEventArg;
                    beforeSuiteEventArg.Id = test.Id;

                    try
                    {
                        if (AfterSuiteStarted != null) AfterSuiteStarted(this, new TestItemStartedEventArgs(Bridge.Service, startSuiteRequest, test.Id));
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Exception was thrown in 'AfterSuiteStarted' subscriber." + Environment.NewLine + exp);
                    }
                }
                else
                {
                    _suitesFlow[id] = beforeSuiteEventArg;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }

        public delegate void SuiteFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event SuiteFinishedHandler BeforeSuiteFinished;
        public static event SuiteFinishedHandler AfterSuiteFinished;

        private void FinishSuite(XmlDocument xmlDoc)
        {
            try
            {
                var id = xmlDoc.SelectSingleNode("/*/@id").Value;
                var result = xmlDoc.SelectSingleNode("/*/@result").Value;
                var parentId = xmlDoc.SelectSingleNode("/*/@parentId");

                // at the end of execution nunit raises 2 the same events, we need only that which has 'parentId' xml tag
                if (parentId != null)
                {
                    if (!_suitesFlow[id].Canceled)
                    {
                        var updateSuiteRequest = new UpdateTestItemRequest();

                        // adding categories to suite
                        var categories = xmlDoc.SelectNodes("//properties/property[@name='Category']");
                        if (categories != null)
                        {
                            updateSuiteRequest.Tags = new List<string>();

                            foreach (XmlNode category in categories)
                            {
                                updateSuiteRequest.Tags.Add(category.Attributes["value"].Value);
                            }
                        }

                        // adding description to suite
                        var description = xmlDoc.SelectSingleNode("//properties/property[@name='Description']");
                        if (description != null)
                        {
                            updateSuiteRequest.Description = description.Attributes["value"].Value;
                        }

                        if (updateSuiteRequest.Description != null || updateSuiteRequest.Tags != null)
                        {
                            Bridge.Service.UpdateTestItem(_suitesFlow[id].Id, updateSuiteRequest);
                        }

                        // finishing suite
                        var finishSuiteRequest = new FinishTestItemRequest
                        {
                            EndTime = DateTime.UtcNow,
                            Status = _statusMap[result]
                        };
                        
                        var eventArg = new TestItemFinishedEventArgs(Bridge.Service, finishSuiteRequest, result, _suitesFlow[id].Id);

                        try
                        {
                            if (BeforeSuiteFinished != null) BeforeSuiteFinished(this, eventArg);
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'BeforeSuiteFinished' subscriber." + Environment.NewLine + exp);
                        }

                        var message = Bridge.Service.FinishTestItem(_suitesFlow[id].Id, finishSuiteRequest).Info;

                        try
                        {
                            if (AfterSuiteFinished != null) AfterSuiteFinished(this, new TestItemFinishedEventArgs(Bridge.Service, finishSuiteRequest, message, _suitesFlow[id].Id));
                        }
                        catch (Exception exp)
                        {
                            Console.WriteLine("Exception was thrown in 'AfterSuiteFinished' subscriber." + Environment.NewLine + exp);
                        }
                    }
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine("ReportPortal exception was thrown." + Environment.NewLine + exception);
            }
        }
    }
}