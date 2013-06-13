﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper_DAL.SqlMaker;
using NUnit.Framework;

namespace TestDapperDal.SqlMakerTest
{
    [TestFixture]
    class SqlMakerInsertTest
    {
        private string _dbScheme;

        [SetUp]
        public void SetUp()
        {
            _dbScheme = "dbo"; // default MS SQl scheme
        }

        [Test]
        public void InitTest()
        {
            var maker = QueryMaker.New(_dbScheme).INSERT("Customer");
            var sql = maker.GetRaw();
            var example = "INSERT INTO\n\t[dbo].[Customer]";
            Assert.That(sql, Is.EqualTo(example).IgnoreCase);
        }

        [Test]
        public void AddColumnTest()
        {
            var maker = QueryMaker.New(_dbScheme)
                .INSERT("Customer")
                    .Col("Name")
                    .Col("Description")
                    .Col("Address");
            var sql = maker.GetRaw();
            var example = "INSERT INTO\n\t[dbo].[Customer] (\n\t\t[Name]\n\t\t, [Description]\n\t\t, [Address]\n\t)";
            Assert.That(sql, Is.EqualTo(example).IgnoreCase);
        }

        [Test]
        public void ValuesTest()
        {
            var maker = QueryMaker.New(_dbScheme)
                .INSERT("Customer")
                    .Col("Name")
                    .Col("Description")
                    .Col("Address")
                .VALUES("@name, @description, @address");
            var sql = maker.GetRaw();
            var example = "INSERT INTO\n\t[dbo].[Customer] (\n\t\t[Name]\n\t\t, [Description]\n\t\t, [Address]\n\t)\n\tVALUES (\n\t\t@name\n\t\t, @description\n\t\t, @address\n\t);";
            Assert.That(sql, Is.EqualTo(example).IgnoreCase);
        }

        [Test]
        public void AddValueTest()
        {
            var maker = QueryMaker.New(_dbScheme)
                .INSERT("Customer")
                    .Col("Name")
                    .Col("Description")
                    .Col("Address")
                    .Col("Zip")
                .VALUES("@name, @description, @address")
                    .Param("zip");
            var sql = maker.GetRaw();
            var example = "INSERT INTO\n\t[dbo].[Customer] (\n\t\t[Name]\n\t\t, [Description]\n\t\t, [Address]\n\t\t, [Zip]\n\t)\n\tVALUES (\n\t\t@name\n\t\t, @description\n\t\t, @address\n\t\t, @zip\n\t);";
            Assert.That(sql, Is.EqualTo(example).IgnoreCase);
        }
    }
}
