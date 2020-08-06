using SUNAward.Data;

using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace SUNAward.Controllers
{
    [Authorize]
    public class PersonsController : ApiController
    {
        // GET api/persons
        [HttpGet]
        public IList<Person> GetPersons(string name)
        {
            return Person.GetPersonByName(name, 5);
        }
    }
}
