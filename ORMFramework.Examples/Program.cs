using System;
using System.Collections.Generic;
using System.Text;
using ORMFramework;
using System.Threading;

namespace ORMFramework.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            ISessionFactory sessionFactory = new SessionFactoryIoc();
            ISession session;
            sessionFactory.Initialize(@"/Users/liuqiming/Projects/personal/SchoolEndORM/ORMFramework.Examples/Config.xml");
            session = sessionFactory.CreateSession();
            string studentId = Guid.NewGuid().ToString();
            TestInsert(session, studentId);
            Console.ReadKey();
            TestUpdate(session, studentId);
            Console.ReadKey();
            //TestDelete(session);
            //Console.ReadKey();
            //TestOneToMany(session);
            //Console.ReadKey();
            //TestManyToOne(session);
            //Console.ReadKey();
            //TestOneToOne(session);
            //Console.ReadKey();
            //TestManyToMany(session);
            //Console.ReadKey();
            //TestMultitransaction(sessionFactory);
            //Console.ReadKey();
        }

        private static void ReadObjects(ISession session)
        {
            Console.WriteLine("Get objects from database...");
            Console.WriteLine("--------------------------------");
            IEnumerable<Student> result = session.Search<Student>("1==1");
            foreach (Student item in result)
            {
                Console.WriteLine("{0}--{1}--{2}", item.StudentId, item.Name, item.Gender);
            }
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Got objects successfully...");
        }

        private static void TestInsert(ISession session, string studentId)
        {
            Student student = new Student();
            Console.WriteLine("Insert an object to database...");
            student.StudentId = studentId;
            student.Name = "Dickson";
            student.Gender = "Male";
            session.Insert(student);
            session.Submit();
            Console.WriteLine("Inserted Successfully...");
            Console.ReadKey();
            ReadObjects(session);
        }

        private static void TestUpdate(ISession session, string studentId)
        {
            Student[] result;
            Console.WriteLine("Update an object...");
            result = session.Search<Student>(string.Format("StudentId=='{0}'", studentId));
            result[0].Name = "Lisa";
            result[0].Gender = "Female";
            session.Update(result[0]);
            session.Submit();
            Console.WriteLine("Updated Successfully...");
            Console.ReadKey();
            ReadObjects(session);
        }

        private static void TestDelete(ISession session)
        {
            Student[] result;
            Console.WriteLine("Delete an object...");
            result = session.Search<Student>("StudentId=='200730740404'");
            session.Delete(result[0]);
            session.Submit();
            Console.WriteLine("Deleted Successfully...");
            Console.ReadKey();
            ReadObjects(session);
        }

        private static void TestOneToMany(ISession session)
        {
            Department[] result;
            Console.WriteLine("Test one-to-many relation...");
            Console.WriteLine("Get an object:");
            result = session.Search<Department>("Id==1");
            Console.WriteLine("{0}--{1}", result[0].Id, result[0].Name);
            Console.WriteLine("Objects refer to the object:");
            foreach (Teacher t in result[0].Teachers)
            {
                Console.WriteLine("{0}--{1}--{2}", t.Id, t.Name, t.Gender);
            }
        }

        private static void TestManyToOne(ISession session)
        {
            Teacher[] result;
            Console.WriteLine("Test many-to-one relation...");
            Console.WriteLine("Get an object:");
            result = session.Search<Teacher>("Id==1");
            Console.WriteLine("{0}--{1}--{2}", result[0].Id, result[0].Name, result[0].Gender);
            Console.WriteLine("Object refers to the object:");
            Console.WriteLine("{0}--{1}", result[0].Department.Id, result[0].Department.Name);
        }

        private static void TestOneToOne(ISession session)
        {
            ContactMenu[] result;
            Console.WriteLine("Test one-to-one relation...");
            Console.WriteLine("Get an object:");
            result = session.Search<ContactMenu>("Email=='t@abc.com'");
            Console.WriteLine("{0}--{1}--{2}", result[0].Phone, result[0].Email, result[0].Address);
            Console.WriteLine("Object refers to the object:");
            Console.WriteLine("{0}--{1}--{2}", result[0].Student.StudentId, result[0].Student.Name, result[0].Student.Gender);
        }

        private static void TestManyToMany(ISession session)
        {
            Course[] result;
            Console.WriteLine("Test many-to-many relation...");
            Console.WriteLine("Get an object;");
            result = session.Search<Course>("Id==1");
            Console.WriteLine("{0}--{1}--{2}", result[0].Id, result[0].Name, result[0].Classroom);
            Console.WriteLine("Objects refer to the object:");
            foreach (Student s in result[0].Students)
            {
                Console.WriteLine("{0}--{1}--{2}", s.StudentId, s.Name, s.Gender);
            }
        }

        private static void TestMultitransaction(ISessionFactory sessionFactory)
        {
            Thread transaction1 = new Thread(new ParameterizedThreadStart(Transaction1));
            transaction1.Start(sessionFactory);
        }

        private static void Transaction1(object sessionFactory)
        {
            ISession session = ((ISessionFactory)sessionFactory).CreateSession();
            Course[] result = session.Search<Course>("Id==2");
            Thread transaction2 = new Thread(new ParameterizedThreadStart(Transaction2));
            result[0].Name = "Art";
            session.Update(result[0]);
            transaction2.Start(sessionFactory);
            Console.WriteLine("Transaction1 updated the object but did not submit...");
            Thread.Sleep(2000);
            session.Submit();
            Console.WriteLine("Transaction1 submited...");
        }

        private static void Transaction2(object sessionFactory)
        {
            ISession session = ((ISessionFactory)sessionFactory).CreateSession();
            Course[] result;
            Console.WriteLine("Transaction2 begin to read the object...");
            result = session.Search<Course>("Id==2");
            Console.WriteLine("Transcation2 finished reading...");
            Console.WriteLine("{0}--{1}--{2}", result[0].Id, result[0].Name, result[0].Classroom);
        }
    }
}