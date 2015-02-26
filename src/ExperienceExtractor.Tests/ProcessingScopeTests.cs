//--------------------------------------------------------------------------------------------
// Copyright 2015 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------
using System;
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Tests
{
    [TestClass]
    public class ProcessingScopeTests
    {
        [TestMethod]
        public void SimpleIndexIncrement()
        {
            var scope = new ProcessingScope();

            scope.Set(new Level1());
            Assert.AreEqual(1, scope.Index<Level1>());
            scope.Set(new Level1());
            Assert.AreEqual(2, scope.Index<Level1>());
        }

        [TestMethod]        
        public void ScopeCanBeNull()
        {
            var scope = new ProcessingScope();
            Assert.IsNull(scope.Current<Level1>());
        }

        [TestMethod]
        public void ReturnCorrectObject()
        {

            var scope = new ProcessingScope();
            var o1 = new Level1();
            var o2 = new Level1();
            var o2_1 = new Level2();

            scope.Set(o1);
            Assert.AreSame(o1, scope.Current<Level1>());

            scope.Set(o2);
            Assert.AreSame(o2, scope.Current<Level1>());

            var childScope = scope.CreateChildScope(new DummyMapper()).Set(o2_1);
            Assert.AreSame(o2_1, childScope.Current<Level2>());
            Assert.AreSame(o2, childScope.Current<Level1>());
        }

        [TestMethod]
        public void TestNesting()
        {
            var scope1 = new ProcessingScope();
            var scope2 = scope1.CreateChildScope(new DummyMapper());
            var scope3 = scope2.CreateChildScope(new DummyMapper());

            var o1 = new Level1();
            var o1_1 = new Level2();
            var o1_1_1 = new Level3();
            var o1_1_2 = new Level3();
            var o1_2 = new Level2();
            var o2 = new Level1();
            var o2_1 = new Level2();
            var o3 = new Level1();
            var o3_1 = new Level2();
            var o3_2 = new Level2();
            var o3_2_1 = new Level3();
            var o3_3 = new Level2();
            var o3_3_1 = new Level3();
            var o3_3_2 = new Level3();

            scope1.Set(o1);
            Assert.AreEqual(1, scope1.GlobalIndex);
            scope2.Set(o1_1);
            Assert.AreEqual(1, scope2.Index<Level1>());
            Assert.AreEqual(1, scope2.Index<Level2>());

            scope3.Set(o1_1_1);
            Assert.AreEqual(1, scope3.Index<Level1>());
            Assert.AreEqual(1, scope3.Index<Level2>());
            Assert.AreEqual(1, scope3.Index<Level3>());
            scope3.Set(o1_1_2);
            Assert.AreEqual(2, scope3.Index<Level1>());
            Assert.AreEqual(2, scope3.Index<Level2>());
            Assert.AreEqual(2, scope3.Index<Level3>());
            Assert.AreEqual(2, scope3.ChildIndex);
            Assert.AreEqual(2, scope3.GlobalIndex);

            scope2.Set(o1_2);
            Assert.AreEqual(2, scope2.Index<Level1>());
            Assert.AreEqual(2, scope2.Index<Level2>());
            Assert.AreEqual(2, scope2.ChildIndex);
            Assert.AreEqual(2, scope3.GlobalIndex);

            scope1.Set(o2);
            Assert.AreEqual(2, scope1.GlobalIndex);
            scope2 = scope2.Set(o2_1);
            Assert.AreEqual(1, scope2.Index<Level1>());
            Assert.AreEqual(3, scope2.GlobalIndex);

            scope1.Set(o3);
            Assert.AreEqual(3, scope1.GlobalIndex);
            scope2 = scope2.Set(o3_1);
            Assert.AreEqual(1, scope2.Index<Level1>());
            Assert.AreEqual(4, scope2.GlobalIndex);

            scope2.Set(o3_2);
            Assert.AreEqual(2, scope2.Index<Level1>());
            Assert.AreEqual(5, scope2.GlobalIndex);

            scope3.Set(o3_2_1);
            Assert.AreEqual(1, scope3.Index<Level1>());
            Assert.AreEqual(1, scope3.Index<Level2>());
            Assert.AreEqual(3, scope3.GlobalIndex);

            scope2.Set(o3_3);
            Assert.AreEqual(3, scope2.Index<Level1>());
            Assert.AreEqual(6, scope2.GlobalIndex);

            scope3.Set(o3_3_1);
            Assert.AreEqual(2, scope3.Index<Level1>());
            Assert.AreEqual(1, scope3.Index<Level2>());
            Assert.AreEqual(4, scope3.GlobalIndex);

            scope3.Set(o3_3_2);
            Assert.AreEqual(3, scope3.Index<Level1>());
            Assert.AreEqual(2, scope3.Index<Level2>());
            Assert.AreEqual(5, scope3.GlobalIndex);

            //Check that the objects are correctly assigned

            Assert.AreSame(o3, scope3.Current<Level1>());
            Assert.AreSame(o3_3, scope3.Current<Level2>());
            Assert.AreSame(o3_3_2, scope3.Current<Level3>());

            Assert.AreSame(o3, scope2.Current<Level1>());
            Assert.AreSame(o3_3, scope2.Current<Level2>());

            Assert.AreSame(o3, scope1.Current<Level1>());
        }


        class Level1 { }
        class Level2 { }
        class Level3 { }

        class DummyMapper : TableMapperBase {
            
            protected override IEnumerable SelectRowItems(ProcessingScope context)
            {
                yield break;
            }
        }
    }
}
