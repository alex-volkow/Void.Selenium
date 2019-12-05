﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;
using Xunit;

namespace Void.Selenium.Tests
{
    public class FixedWebPointerTests : WebContext
    {
        public FixedWebPointerTests() {
            OpenDefaultPage();
        }

        [Fact]
        public void FindExistingElement() {
            var pointer = CreateWebPointer();
            pointer.Match();
            Assert.True(pointer.IsMatched);
        }

        [Fact]
        public void FindNonExistingElement() {
            var pointer = CreateWebPointer("//body1");
            pointer.Match();
            Assert.True(!pointer.IsMatched);
        }

        [Fact]
        public void FindStaledElement() {
            var pointer = CreateWebPointer();
            pointer.Match();
            Assert.True(pointer.IsMatched);
            OpenDefaultPage();
            Assert.True(pointer.IsStaled);
        }

        [Fact]
        public void AutoUpdateElement() {
            var pointer = CreateWebPointer();
            pointer.Match();
            Assert.True(pointer.IsMatched);
            OpenDefaultPage();
            Assert.True(pointer.IsStaled);
            Assert.NotNull(pointer.Required());
        }

        [Fact]
        public void EqualElements() {
            var pointer = CreateWebPointer();
            var first = pointer.Match();
            var second = pointer.Match();
            Assert.Equal(first, second);
        }

        [Fact]
        public void EqualStaledElements() {
            var pointer = CreateWebPointer();
            var first = pointer.Match();
            OpenDefaultPage();
            var second = pointer.Match();
            Assert.NotEqual(first, second);
        }

        private FixedWebPointer CreateWebPointer(string xpath = "//body") {
            return new FixedWebPointer(GetDriver(), By.XPath(xpath));
        }
    }
}
