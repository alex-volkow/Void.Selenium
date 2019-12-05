﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;
using Void.Reflection;

namespace Void.Selenium
{
    internal abstract class WebPageReflectionElement : IWebPageElement
    {
        protected readonly MemberInfo member;



        public string Name => this.member.Name;

        public object Page { get; }
        
        /// <summary>
        /// Search container.
        /// </summary>
        public ISearchContext Context { get; }

        /// <summary>
        /// Found element.
        /// </summary>
        public IWebElement WrappedElement {
            get => GetMemberValue();
            set => SetMemberValue(value);
        }

        /// <summary>
        /// Element is found but not visible in UI.
        /// </summary>
        public bool IsFoundButNotVisible { get; private set; }


        /// <summary>
        /// Element can be skipped in matching.
        /// </summary>
        public bool IsOptional => this.member.GetCustomAttribute<OptionalAttribute>() != null;

        /// <summary>
        /// Element must be visible in UI.
        /// </summary>
        public bool IsVisible => this.member.GetCustomAttribute<VisibleAttribute>() != null;

        /// <summary>
        /// Check is element found and not stale.
        /// </summary>
        public bool IsMatched => this.WrappedElement != null && !this.IsStaled;

        /// <summary>
        /// Check status of element pointer. If element is stale, need to match it. 
        /// </summary>
        public bool IsStaled {
            get {
                try {
                    return this.WrappedElement != null
                        ? this.WrappedElement.Displayed && false
                        : true;
                }
                catch (StaleElementReferenceException) {
                    return true;
                }
            }
        }



        public WebPageReflectionElement(ISearchContext context, MemberInfo member, object page) {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.member = member ?? throw new ArgumentNullException(nameof(member));
            this.Page = page ?? throw new ArgumentNullException(nameof(page));
        }


        /// <summary>
        /// Provides access to alive element or throw exception.
        /// If element is found and not stale, returns the element.
        /// </summary>
        /// <returns>Match element.</returns>
        /// <exception cref="NotFoundException">Failed to find a matching element.</exception>
        public IWebElement Required() {
            if (this.IsMatched) {
                return this.WrappedElement;
            }
            return Match() ?? throw new NotFoundException(
                $"Element is not found: {this.member.Name} " +
                $"({this.Page.GetType().GetNameWithNamespaces()})"
                );
        }

        /// <summary>
        /// Try to find a web element using condition builded from 
        /// 'XPath', 'FindsBy', 'FindsByAll', 'FindsBySequence' and 'Visible' attributes.
        /// </summary>
        public IWebElement Match() {
            this.WrappedElement = null;
            this.IsFoundButNotVisible = false;
            var allCondition = this.member.GetCustomAttribute<FindsByAllAttribute>();
            var sequenceCondition = this.member.GetCustomAttribute<FindsBySequenceAttribute>();
            if (allCondition != null && sequenceCondition != null) {
                var message = new StringBuilder();
                message.Append("Invalid combination ");
                message.Append('\'').Append(nameof(FindsByAllAttribute)).Append('\'');
                message.Append(" and ");
                message.Append('\'').Append(nameof(FindsBySequenceAttribute)).Append('\'');
                message.Append(" attributes on ");
                message.Append('\'').Append(this.member.Name).Append('\'');
                message.Append(" member of ");
                message.Append('\'').Append(this.Page.GetType().GetNameWithAssemblies()).Append('\'');
                message.Append(" class");
                throw new NotSupportedException(message.ToString());
            }
            var xpathLocators = this.member.GetCustomAttributes<XPathAttribute>();
            var standardLocators = this.member.GetCustomAttributes<FindsByAttribute>().ToList();
            foreach (var xpath in xpathLocators) {
                standardLocators.Add(new FindsByAttribute {
                    CustomFinderType = xpath.CustomFinderType,
                    Priority = xpath.Priority,
                    Using = xpath.XPath,
                    How = How.XPath,
                });
            }
            var result = default(IWebElement);
            if (allCondition != null) {
                result = FindsByAll(standardLocators);
            }
            else if (sequenceCondition != null) {
                result = FindsBySequence(standardLocators);
            }
            else {
                result = FindsByAny(standardLocators);
            }
            if (result == null) {
                return null;
            }
            if (this.IsVisible && !result.IsVisible()) {
                this.IsFoundButNotVisible = true;
                return null;
            }
            this.WrappedElement = result;
            return result;
        }

        public override string ToString() {
            return this.Name;
        }

        public override int GetHashCode() {
            return HashCode.Create(
                this.Page.GetType(),
                this.Name
                );
        }

        public override bool Equals(object obj) {
            if (obj is WebPageReflectionElement other) {
                return other.Page.GetType() == this.Page.GetType()
                    && other.Name == this.Name;
            }
            return false;
        }

        protected abstract IWebElement GetMemberValue();

        protected abstract void SetMemberValue(IWebElement element);

        private IWebElement FindsByAll(IEnumerable<FindsByAttribute> attributes) {
            var elements = attributes
                .Select(e => this.Context.FindElements(GetLocator(e)).FirstOrDefault())
                .ToArray();
            if (elements.Length == 0) {
                return null;
            }
            if (elements.Any(e => e == null)) {
                return null;
            }
            var last = default(IWebElement);
            foreach (var element in elements) {
                if (last != null && !element.Equals(last)) {
                    return null;
                }
                last = element;
            }
            return last;
        }

        private IWebElement FindsBySequence(IEnumerable<FindsByAttribute> attributes) {
            var context = this.Context;
            foreach (var locator in GetSequence(attributes)) {
                context = context.FindElements(locator).FirstOrDefault();
                if (context == null) {
                    return null;
                }
            }
            if (context == this.Context) {
                return null;
            }
            return (IWebElement)context;
        }

        private IWebElement FindsByAny(IEnumerable<FindsByAttribute> attributes) {
            var groups = attributes.GroupBy(e => e.Priority).OrderBy(e => e.Key);
            foreach (var group in groups) {
                foreach (var attribute in group) {
                    var locator = GetLocator(attribute);
                    var element = this.Context.FindElements(locator).FirstOrDefault();
                    if (element != null) {
                        return element;
                    }
                }
            }
            return null;
        }

        private IEnumerable<By> GetSequence(IEnumerable<FindsByAttribute> attributes) {
            var groups = attributes.GroupBy(e => e.Priority).OrderBy(e => e.Key);
            var first = true;
            foreach (var group in groups) {
                foreach (var attribute in group) {
                    if (first) {
                        yield return GetLocator(attribute);
                        first = false;
                    }
                    else {
                        yield return GetLocator(attribute, e => {
                            return e.StartsWith(".") ? e : $".{e}";
                        });
                    }
                }
            }
        }

        private By GetLocator(FindsByAttribute attribute, Func<string, string> formatter = null) {
            if (attribute.How == How.Custom) {
                if (attribute.CustomFinderType == null) {
                    throw new ArgumentNullException(
                        $"Required '{nameof(attribute.CustomFinderType)}' finder"
                        );
                }
                return (By)Activator.CreateInstance(
                    attribute.CustomFinderType,
                    new object[] { attribute.Using }
                    );
            }
            if (formatter == null) {
                formatter = e => e;
            }
            return GetStandardLocator(attribute)(formatter(attribute.Using));
        }

        private Func<string, By> GetStandardLocator(FindsByAttribute attribute) {
            switch (attribute.How) {
                case How.Id: return By.Id;
                case How.Name: return By.Name;
                case How.XPath: return By.XPath;
                case How.ClassName: return By.ClassName;
                case How.CssSelector: return By.CssSelector;
                case How.LinkText: return By.LinkText;
                case How.PartialLinkText: return By.PartialLinkText;
                case How.TagName: return By.TagName;
                default: throw new NotSupportedException(
                    $"Unsipported locator: {attribute.How}"
                    );
            }
        }
    }
}
