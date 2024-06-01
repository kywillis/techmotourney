import "../models/request/createTournamentRequest.model.tsp";
import "../models/request/updateTournamentRequest.model.tsp";
import "../models/tournament.model.tsp";
import "@typespec/http";

using TypeSpec.Http;
using TypeSpec.Rest;

namespace Tournaments {
    
    @route("/tournaments")
    interface Tournaments {        
        @get listAll(): Tournament[];
        
        @get listResultsByPlayer(@path  @segment("player") playerId: int32): Player;

        @post addTournament(@body tournament: CreateTournamentRequest): void;

        @put updateTournament(@path tournamentId: int32, @body tournament: UpdateTournamentRequest): void;
        
        @delete deleteTournament(@path tournamentId: int32): void;
    }
}