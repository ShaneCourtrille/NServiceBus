﻿namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Core.Tests.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class RegisterStepTests
    {
        [Test]
        public void Replace_WhenStepIdsDoNotMatch_ShouldThrowInvalidOperationException()
        {
            var registerStep = RegisterStep.Create("stepId 1", typeof(BehaviorA), "description");
            var replacement = new ReplaceStep("stepId 2", typeof(BehaviorB));

            Assert.Throws<InvalidOperationException>(() => registerStep.Replace(replacement));
        }

        [Test]
        public void Replace_ShouldReplaceBehaviorType()
        {
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description");
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

            registerStep.Replace(replacement);

            Assert.AreEqual(typeof(BehaviorB), registerStep.BehaviorType);
        }

        [Test]
        public void Replace_WhenReplacementContainsNoDescription_ShouldKeepOriginalDescription()
        {
            const string originalDescription = "description";
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), originalDescription);
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

            registerStep.Replace(replacement);

            Assert.AreEqual(originalDescription, registerStep.Description);
        }

        [Test]
        public void Replace_WhenReplacementContainsEmptyDescription_ShouldKeepOriginalDescription()
        {
            const string originalDescription = "description";
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), originalDescription);
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), "    ");

            registerStep.Replace(replacement);

            Assert.AreEqual(originalDescription, registerStep.Description);
        }

        [Test]
        public void Replace_WhenReplacementContainsDescription_ShouldReplaceDescription()
        {
            const string replacementDescription = "new";
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description");
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), replacementDescription);

            registerStep.Replace(replacement);

            Assert.AreEqual(replacementDescription, registerStep.Description);
        }

        [Test]
        public void Replace_WhenReplacementProvidesNoFactory_ShouldBuildReplacementFromBuilder()
        {
            var originalBehaviorFactoryCalled = false;
            Func<IBuilder, IBehavior> originalBehaviorFactory = b =>
            {
                originalBehaviorFactoryCalled = true;
                return new BehaviorA();
            };

            var builder = new FakeBuilder(typeof(BehaviorB));
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description", originalBehaviorFactory);
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB));

            registerStep.Replace(replacement);
            var behavior = registerStep.CreateBehavior(builder);

            Assert.IsFalse(originalBehaviorFactoryCalled);
            Assert.AreEqual(typeof(BehaviorB), behavior.Type);
        }

        [Test]
        public void Replace_WhenReplacementProvidedFactory_ShouldBuildReplacementFromFactory()
        {
            var replacementBehaviorFactoryCalled = false;
            Func<IBuilder, IBehavior> replacementBehaviorFactory = b =>
            {
                replacementBehaviorFactoryCalled = true;
                return new BehaviorA();
            };

            var builder = new FakeBuilder(typeof(BehaviorB));
            var registerStep = RegisterStep.Create("pipelineStep", typeof(BehaviorA), "description", b => { throw new Exception(); });
            var replacement = new ReplaceStep("pipelineStep", typeof(BehaviorB), factoryMethod: replacementBehaviorFactory);

            registerStep.Replace(replacement);
            var behavior = registerStep.CreateBehavior(builder);

            Assert.IsTrue(replacementBehaviorFactoryCalled);
            Assert.AreEqual(typeof(BehaviorB), behavior.Type);
        }

        class BehaviorA : Behavior<IRoutingContext>
        {
            public override Task Invoke(IRoutingContext context, Func<Task> next)
            {
                return TaskEx.CompletedTask;
            }
        }

        class BehaviorB : Behavior<IRoutingContext>
        {
            public override Task Invoke(IRoutingContext context, Func<Task> next)
            {
                return TaskEx.CompletedTask;
            }
        }
    }
}