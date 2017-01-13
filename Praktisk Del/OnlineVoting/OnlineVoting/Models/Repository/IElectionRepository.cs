using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data;
using System.Data.Entity;

namespace OnlineVoting.Models.Repository
{
    
    public interface IElectionRepository : IDisposable// min interface, IDisposable används för att rensa upp ohanterade resurser, Ohanterade resurser är till exempel, öppna filer, Öppna nätverksanslutningar, Datorstyrda minne
    {
        //Marco, bra att repetera 
        //klasser som implementerar interfacen funktionaliteten kan använda interfacens egenskaper, metoder och/eller händelser. 
        //interface i C# är ett sätt att komma runt bristen på multipelt arv i C #, vilket innebär att man inte inte kan ärva från flera klasser C# men du kan inplämnetar flera gränssnitt istället i C#.

        void Save();
        DbContextTransaction Transaction();
        void VotingDetailAdd(VotingDetail votingDetail);

        Voting GetElectionById(int ElectionID);
        Candidate GetListOfAllElectionCandidates(int ElectionID);
        Candidate GetCandidateById(int ElectionID);
        void DeleteCandidate(Candidate candidate);
        void UpdateCandidate(Candidate candidate);
        void AddCandidate(Candidate candidate);
        Candidate GetCandidateByElectionIdAndUserId(int ElectionID, int UserId);

        List<Rank> ShowResultsOfElectionById(int ElectionID);
        Voting GetElectionByIdNoTracking(int ElectionId);
        void AddElection(Voting voting);
        List<Voting> GetListOfAllElectionsById(int id);
        void DeleteElection(Voting Election);
        List<Voting> GetListOfAllElections();
        List<string> GetElectionByNameForAutocomplete(string SearchText);
        List<Voting> GetElectionByName(string SearchText);
        List<Voting> GetListOfElectionIfOpen(State state);
        VotingDetail GetIfUserAlreadyVotedInElection(int VotingId, int UserId);
        List<Voting> GetElectionByStateId(State state);
        List<Voting> GetElectionByYear(int year);
        List<Voting> GetElectionByMonths(int MonthsNum);
        List<Voting> GetElectionByYearandMonths(int year, int MonthsNum);
        List<Voting> GetElectionByNameAndStateId(string SearchText, State state);
        List<string> GetElectionByNameAndStateIdForAutocomplete(string SearchText, State state);
        List<Voting> GetElectionByYearAndStateId(int year, State state);
        List<Voting> GetElectionByMonthsAndStateId(int MonthsNum, State state);
        List<Voting> GetElectionByYearMonthsAndStateId(int year, int MonthsNum, State state);

        void UpdateElection(Voting voting);
        
    }
}
