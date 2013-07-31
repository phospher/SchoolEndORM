using System;
using System.Collections.Generic;
using System.Text;

namespace ORMFramework.Examples {
    public class ContactMenu {
        private int _id;
        private string _email;
        private string _phone;
        private string _address;
        private Student _student;

        public int Id {
            get { return _id; }
            set { _id = value; }
        }

        public string Email {
            get { return _email; }
            set { _email = value; }
        }

        public string Phone {
            get { return _phone; }
            set { _phone = value; }
        }

        public string Address {
            get { return _address; }
            set { _address = value; }
        }

        public Student Student {
            get { return _student; }
            set { _student = value; }
        }
    }
}
