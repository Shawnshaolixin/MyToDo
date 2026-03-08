using System;
using System.Collections.Generic;
using System.Text;

namespace MyToDo.Common.Models
{
	public class BaseDto:BindableBase
	{
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }
		private DateTime createDate;

		public DateTime CreateDate
		{
			get { return createDate; }
			set { createDate = value; }
		}
        private DateTime updateDate;

        public DateTime UpdateDate
        {
            get { return updateDate; }
            set { updateDate = value; }
        }
    }
    public class ToDoDto: BaseDto
    {

		
		private string  title;

		public string  Title
		{
			get { return title; }
			set { title = value; }
		}
		private string content;

		public string Content
		{
			get { return content; }
			set { content = value; }
		}
		private int status;

		public int Status
		{
			get { return status; }
			set { status = value; }
		}

	}
}
