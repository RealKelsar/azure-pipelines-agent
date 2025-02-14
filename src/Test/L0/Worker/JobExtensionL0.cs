// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;
using System.Threading;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Worker.Telemetry;
using Microsoft.VisualStudio.Services.WebPlatform;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class JobExtensionL0
    {
        private class TestJobExtension : JobExtension
        {
            public override HostTypes HostType => HostTypes.None;

            public override Type ExtensionType => typeof(IJobExtension);

            public override void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath)
            {
                repoName = "";
                sourcePath = "";
            }

            public override IStep GetExtensionPostJobStep(IExecutionContext jobContext)
            {
                return null;
            }

            public override IStep GetExtensionPreJobStep(IExecutionContext jobContext)
            {
                return null;
            }

            public override string GetRootedPath(IExecutionContext context, string path)
            {
                return path;
            }

            public override void InitializeJobExtension(IExecutionContext context, IList<Pipelines.JobStep> steps, Pipelines.WorkspaceOptions workspace)
            {
                return;
            }
        }

        private IExecutionContext _jobEc;
        private Pipelines.AgentJobRequestMessage _message;
        private Mock<ITaskManager> _taskManager;
        private Mock<IAgentLogPlugin> _logPlugin;
        private Mock<IJobServerQueue> _jobServerQueue;
        private Mock<IVstsAgentWebProxy> _proxy;
        private Mock<IAgentCertificateManager> _cert;
        private Mock<IConfigurationStore> _config;
        private Mock<IPagingLogger> _logger;
        private Mock<IExpressionManager> _express;
        private Mock<IContainerOperationProvider> _containerProvider;
        private Mock<ICustomerIntelligenceServer> _mockCiService;
        private Mock<IAsyncCommandContext> _asyncCommandContext;
        private TestHostContext CreateTestContext(CancellationTokenSource _tokenSource, [CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _jobEc = new Agent.Worker.ExecutionContext();
            _taskManager = new Mock<ITaskManager>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _config = new Mock<IConfigurationStore>();
            _logger = new Mock<IPagingLogger>();
            _proxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();
            _express = new Mock<IExpressionManager>();
            _containerProvider = new Mock<IContainerOperationProvider>();
            _logPlugin = new Mock<IAgentLogPlugin>();

            TaskRunner step1 = new TaskRunner();
            TaskRunner step2 = new TaskRunner();
            TaskRunner step3 = new TaskRunner();
            TaskRunner step4 = new TaskRunner();
            TaskRunner step5 = new TaskRunner();
            TaskRunner step6 = new TaskRunner();
            TaskRunner step7 = new TaskRunner();
            TaskRunner step8 = new TaskRunner();
            TaskRunner step9 = new TaskRunner();
            TaskRunner step10 = new TaskRunner();
            TaskRunner step11 = new TaskRunner();
            TaskRunner step12 = new TaskRunner();

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));
            var settings = new AgentSettings
            {
                AgentId = 1,
                AgentName = "agent1",
                ServerUrl = "https://test.visualstudio.com",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            _config.Setup(x => x.GetSetupInfo())
                .Returns(new List<SetupInfo>());

            _proxy.Setup(x => x.ProxyAddress)
                            .Returns(string.Empty);

            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());
            JobEnvironment environment = new JobEnvironment();
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            environment.SystemConnection = new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://test.visualstudio.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                }
            };
            environment.SystemConnection.Authorization.Parameters["AccessToken"] = "token";

            List<TaskInstance> tasks = new List<TaskInstance>()
            {
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task1",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task2",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task3",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task4",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task5",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task6",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task7",
                },
            };

            Guid JobId = Guid.NewGuid();
            _message = Pipelines.AgentJobRequestMessageUtil.Convert(new AgentJobRequestMessage(plan, timeline, JobId, testName, testName, environment, tasks));

            _taskManager.Setup(x => x.DownloadAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.TaskStep>>()))
                .Returns(Task.CompletedTask);

            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task1")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task2")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task3")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task4")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task5")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task6")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task7")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });

            hc.SetSingleton(_taskManager.Object);
            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_proxy.Object);
            hc.SetSingleton(_cert.Object);
            hc.SetSingleton(_express.Object);
            hc.SetSingleton(_containerProvider.Object);
            hc.SetSingleton(_logPlugin.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // jobcontext logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // init step logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 12

            hc.EnqueueInstance<ITaskRunner>(step1);
            hc.EnqueueInstance<ITaskRunner>(step2);
            hc.EnqueueInstance<ITaskRunner>(step3);
            hc.EnqueueInstance<ITaskRunner>(step4);
            hc.EnqueueInstance<ITaskRunner>(step5);
            hc.EnqueueInstance<ITaskRunner>(step6);
            hc.EnqueueInstance<ITaskRunner>(step7);
            hc.EnqueueInstance<ITaskRunner>(step8);
            hc.EnqueueInstance<ITaskRunner>(step9);
            hc.EnqueueInstance<ITaskRunner>(step10);
            hc.EnqueueInstance<ITaskRunner>(step11);
            hc.EnqueueInstance<ITaskRunner>(step12);

            _jobEc.Initialize(hc);
            _jobEc.InitializeJob(_message, _tokenSource.Token);
            return hc;
        }

        private TestHostContext CreateMSITestContext(CancellationTokenSource _tokenSource, [CallerMemberName] String testName = "")
        {
            TestHostContext hc = new TestHostContext(this, testName);
            _jobEc = new Agent.Worker.ExecutionContext();
            _taskManager = new Mock<ITaskManager>();
            _jobServerQueue = new Mock<IJobServerQueue>();
            _config = new Mock<IConfigurationStore>();
            _logger = new Mock<IPagingLogger>();
            _proxy = new Mock<IVstsAgentWebProxy>();
            _cert = new Mock<IAgentCertificateManager>();
            _express = new Mock<IExpressionManager>();
            _containerProvider = new Mock<IContainerOperationProvider>();
            _logPlugin = new Mock<IAgentLogPlugin>();
            _mockCiService = new Mock<ICustomerIntelligenceServer>();
            _asyncCommandContext = new Mock<IAsyncCommandContext>();


            TaskRunner step1 = new TaskRunner();
            TaskRunner step2 = new TaskRunner();
            TaskRunner step3 = new TaskRunner();
            TaskRunner step4 = new TaskRunner();
            TaskRunner step5 = new TaskRunner();
            TaskRunner step6 = new TaskRunner();
            TaskRunner step7 = new TaskRunner();
            TaskRunner step8 = new TaskRunner();
            TaskRunner step9 = new TaskRunner();
            TaskRunner step10 = new TaskRunner();
            TaskRunner step11 = new TaskRunner();
            TaskRunner step12 = new TaskRunner();

            _logger.Setup(x => x.Setup(It.IsAny<Guid>(), It.IsAny<Guid>()));
            AgentSettings settings = new AgentSettings
            {
                AgentId = 1,
                AgentName = "agent1",
                ServerUrl = "https://test.visualstudio.com",
                WorkFolder = "_work",
            };

            _config.Setup(x => x.GetSettings())
                .Returns(settings);

            _config.Setup(x => x.GetSetupInfo())
                .Returns(new List<SetupInfo>());

            _proxy.Setup(x => x.ProxyAddress)
                            .Returns(string.Empty);

            TaskOrchestrationPlanReference plan = new TaskOrchestrationPlanReference();
            TimelineReference timeline = new Timeline(Guid.NewGuid());
            JobEnvironment environment = new JobEnvironment();
            environment.Variables[Constants.Variables.System.Culture] = "en-US";
            environment.SystemConnection = new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://test.visualstudio.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "OAuth",
                }
            };
            environment.SystemConnection.Authorization.Parameters["AccessToken"] = "token";

            List<TaskInstance> tasks = new List<TaskInstance>()
            {
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task1",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task2",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task3",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task4",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task5",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task6",
                },
                new TaskInstance()
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "task7",
                },
            };

            Guid JobId = Guid.NewGuid();
            _message = Pipelines.AgentJobRequestMessageUtil.Convert(new AgentJobRequestMessage(plan, timeline, JobId, testName, testName, environment, tasks));

            _taskManager.Setup(x => x.DownloadAsync(It.IsAny<IExecutionContext>(), It.IsAny<IEnumerable<Pipelines.TaskStep>>()))
                .Returns(Task.CompletedTask);

            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task1")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task2")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task3")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task4")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = null,
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task5")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = null,
                        PostJobExecution = new ExecutionData(),
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task6")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = new ExecutionData(),
                        Execution = new ExecutionData(),
                        PostJobExecution = null,
                    },
                });
            _taskManager.Setup(x => x.Load(It.Is<Pipelines.TaskStep>(t => t.DisplayName == "task7")))
                .Returns(new Definition()
                {
                    Data = new DefinitionData()
                    {
                        PreJobExecution = null,
                        Execution = new ExecutionData(),
                        PostJobExecution = new ExecutionData(),
                    },
                });

            hc.SetSingleton(_taskManager.Object);
            hc.SetSingleton(_config.Object);
            hc.SetSingleton(_jobServerQueue.Object);
            hc.SetSingleton(_proxy.Object);
            hc.SetSingleton(_cert.Object);
            hc.SetSingleton(_express.Object);
            hc.SetSingleton(_containerProvider.Object);
            hc.SetSingleton(_logPlugin.Object);
            hc.SetSingleton(_mockCiService.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // jobcontext logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // init step logger
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 1
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object); // step 12

            hc.EnqueueInstance<ITaskRunner>(step1);
            hc.EnqueueInstance<ITaskRunner>(step2);
            hc.EnqueueInstance<ITaskRunner>(step3);
            hc.EnqueueInstance<ITaskRunner>(step4);
            hc.EnqueueInstance<ITaskRunner>(step5);
            hc.EnqueueInstance<ITaskRunner>(step6);
            hc.EnqueueInstance<ITaskRunner>(step7);
            hc.EnqueueInstance<ITaskRunner>(step8);
            hc.EnqueueInstance<ITaskRunner>(step9);
            hc.EnqueueInstance<ITaskRunner>(step10);
            hc.EnqueueInstance<ITaskRunner>(step11);
            hc.EnqueueInstance<ITaskRunner>(step12);

            _jobEc.Initialize(hc);
            _jobEc.InitializeJob(_message, _tokenSource.Token);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionSetupInfo()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            using (TestHostContext hc = CreateTestContext(tokenSource))
            {
                var config = hc.GetService<IConfigurationStore>();
                var setupInfo = (List<SetupInfo>)config.GetSetupInfo();
                setupInfo.Add(new SetupInfo()
                {
                    Group = "Test Group",
                    Detail = "Test Detail"
                });
                setupInfo.Add(new SetupInfo()
                {
                    Detail = "Environment: test\nVersion: 123"
                });


                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                await testExtension.InitializeJob(_jobEc, _message);

                _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "##[group]Test Group", It.IsAny<long>()), Times.Exactly(1));
                _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Test Detail", It.IsAny<long>()), Times.Exactly(1));

                _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "##[group]Machine Setup Info", It.IsAny<long>()), Times.Exactly(1));
                _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Environment: test", It.IsAny<long>()), Times.Exactly(1));
                _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Version: 123", It.IsAny<long>()), Times.Exactly(1));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionSetupInfoMSI()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateMSITestContext(tokenSource);
            IConfigurationStore config = hc.GetService<IConfigurationStore>();
            List<SetupInfo> setupInfo = (List<SetupInfo>)config.GetSetupInfo();
            setupInfo.Add(new SetupInfo()
            {
                Group = "Test Group",
                Detail = "Test Detail"
            });
            setupInfo.Add(new SetupInfo()
            {
                Detail = "Environment: test\nVersion: 123"
            });


            TestJobExtension testExtension = new TestJobExtension();
            testExtension.Initialize(hc);
            await testExtension.InitializeJob(_jobEc, _message);

            _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "##[group]Test Group", It.IsAny<long>()), Times.Exactly(1));
            _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Test Detail", It.IsAny<long>()), Times.Exactly(1));

            _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "##[group]Machine Setup Info", It.IsAny<long>()), Times.Exactly(1));
            _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Environment: test", It.IsAny<long>()), Times.Exactly(1));
            _jobServerQueue.Verify(x => x.QueueWebConsoleLine(It.IsAny<Guid>(), "Version: 123", It.IsAny<long>()), Times.Exactly(1));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensioBuildStepsList()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateTestContext(tokenSource);
            TestJobExtension testExtension = new TestJobExtension();
            testExtension.Initialize(hc);
            List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

            var trace = hc.GetTrace();

            trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

            Assert.Equal(12, result.Count);

            Assert.Equal("task2", result[0].DisplayName);
            Assert.Equal("task3", result[1].DisplayName);
            Assert.Equal("task4", result[2].DisplayName);
            Assert.Equal("task6", result[3].DisplayName);
            Assert.Equal("task1", result[4].DisplayName);
            Assert.Equal("task2", result[5].DisplayName);
            Assert.Equal("task6", result[6].DisplayName);
            Assert.Equal("task7", result[7].DisplayName);
            Assert.Equal("task7", result[8].DisplayName);
            Assert.Equal("task5", result[9].DisplayName);
            Assert.Equal("task3", result[10].DisplayName);
            Assert.Equal("task2", result[11].DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensioBuildStepsListMSI()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateMSITestContext(tokenSource);
            TestJobExtension testExtension = new TestJobExtension();
            testExtension.Initialize(hc);
            List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

            var trace = hc.GetTrace();

            trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

            Assert.Equal(12, result.Count);

            Assert.Equal("task2", result[0].DisplayName);
            Assert.Equal("task3", result[1].DisplayName);
            Assert.Equal("task4", result[2].DisplayName);
            Assert.Equal("task6", result[3].DisplayName);
            Assert.Equal("task1", result[4].DisplayName);
            Assert.Equal("task2", result[5].DisplayName);
            Assert.Equal("task6", result[6].DisplayName);
            Assert.Equal("task7", result[7].DisplayName);
            Assert.Equal("task7", result[8].DisplayName);
            Assert.Equal("task5", result[9].DisplayName);
            Assert.Equal("task3", result[10].DisplayName);
            Assert.Equal("task2", result[11].DisplayName);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionIntraTaskState()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateTestContext(tokenSource);
            TestJobExtension testExtension = new TestJobExtension();
            testExtension.Initialize(hc);
            List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

            var trace = hc.GetTrace();

            trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

            Assert.Equal(12, result.Count);

            result[0].ExecutionContext.TaskVariables.Set("state1", "value1", false);
            Assert.Equal("value1", result[5].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Equal("value1", result[11].ExecutionContext.TaskVariables.Get("state1"));

            Assert.Null(result[4].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[1].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[2].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[10].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[6].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[7].ExecutionContext.TaskVariables.Get("state1"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task JobExtensionIntraTaskStateMSI()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateMSITestContext(tokenSource);
            TestJobExtension testExtension = new TestJobExtension();
            testExtension.Initialize(hc);
            List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

            var trace = hc.GetTrace();

            trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

            Assert.Equal(12, result.Count);

            result[0].ExecutionContext.TaskVariables.Set("state1", "value1", false);
            Assert.Equal("value1", result[5].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Equal("value1", result[11].ExecutionContext.TaskVariables.Get("state1"));

            Assert.Null(result[4].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[1].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[2].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[10].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[6].ExecutionContext.TaskVariables.Get("state1"));
            Assert.Null(result[7].ExecutionContext.TaskVariables.Get("state1"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async Task JobExtensionManagementScriptStep()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateTestContext(tokenSource);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);

            Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "C:\\init.ps1");
            Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "C:\\cleanup.ps1");
            Environment.SetEnvironmentVariable("DEBUG_MSI_LOGIN_INFO", "1");

            try
            {
                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

                Assert.Equal(14, result.Count);

                Assert.True(result[0] is ManagementScriptStep);
                Assert.True(result[13] is ManagementScriptStep);

                Assert.Equal(result[0].DisplayName, "Agent Initialization");
                Assert.Equal(result[13].DisplayName, "Agent Cleanup");
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "");
                Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async Task JobExtensionManagementScriptStepMSI()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateMSITestContext(tokenSource);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);
            hc.EnqueueInstance<IPagingLogger>(_logger.Object);

            Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "C:\\init.ps1");
            Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "C:\\cleanup.ps1");
            Environment.SetEnvironmentVariable("DEBUG_MSI_LOGIN_INFO", "1");

            try
            {
                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                List<IStep> result = await testExtension.InitializeJob(_jobEc, _message);

                var trace = hc.GetTrace();

                trace.Info(string.Join(", ", result.Select(x => x.DisplayName)));

                Assert.Equal(14, result.Count);

                Assert.True(result[0] is ManagementScriptStep);
                Assert.True(result[13] is ManagementScriptStep);

                Assert.Equal(result[0].DisplayName, "Agent Initialization");
                Assert.Equal(result[13].DisplayName, "Agent Cleanup");
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK", "");
                Environment.SetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK", "");
            }
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [Trait("SkipOn", "darwin")]
        [Trait("SkipOn", "linux")]
        public async Task JobExtensionTelemetryPublisherSecretValue()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using TestHostContext hc = CreateMSITestContext(tokenSource);

            hc.EnqueueInstance<IAsyncCommandContext>(_asyncCommandContext.Object);
            hc.EnqueueInstance<IAsyncCommandContext>(_asyncCommandContext.Object);
            hc.EnqueueInstance<IAsyncCommandContext>(_asyncCommandContext.Object);

            hc.SetSingleton(new TaskRestrictionsChecker() as ITaskRestrictionsChecker);

            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://admin:password@localhost.com");

                var expectedEvent = new Dictionary<string, object>()
                {
                    { "JobId", null },
                    { "ProxyAddress-${http_proxy}", "http://admin:***@localhost.com"},
                };

                var actualEvents = new List<CustomerIntelligenceEvent[]>();

                _mockCiService.Setup(s => s.PublishEventsAsync(It.IsAny<CustomerIntelligenceEvent[]>()))
                    .Callback<CustomerIntelligenceEvent[]>(actualEvents.Add)
                    .Returns(Task.CompletedTask);


                TestJobExtension testExtension = new TestJobExtension();
                testExtension.Initialize(hc);
                await testExtension.InitializeJob(_jobEc, _message);

                var result = actualEvents.Where(w => w[0].Properties.ContainsKey("ProxyAddress-${http_proxy}"));

                Assert.True(result?.Count() == 1);

                Assert.True(
                    !expectedEvent.Except(result.First()[0].Properties).Any(),
                    $"Event does not match. " +
                    $"Expected:{JsonSerializer.Serialize(expectedEvent)};" +
                    $"Actual:{JsonSerializer.Serialize(result.First()[0].Properties)}"
                );
            }
            finally
            {
                Environment.SetEnvironmentVariable("http_proxy", "");
            }
        }
    }
}