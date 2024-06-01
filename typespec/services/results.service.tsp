import "../models/request/createGameResultRequest.model.tsp";
import "../models/gameResult.model.tsp";
import "@typespec/http";

using TypeSpec.Http;
using TypeSpec.Rest;

namespace Results {
    
    @route("/results")
    interface GameResult {        
        @get listResultsByTournament(@path @segment("tournament") tourneyId: int32): GameResult[];
        
        @get listResultsByPlayer(@path @segment("player") playerId: int32): GameResult[];

        @get search(@query player1Id: int32, @query player2Id: int32): GameResult[];

        @post addGameResult(@body gameResult: CreateGameResultRequest): void;

        @put updateGameResult(@path gameResultId: int32, @body gameResult: GameResult): void;
        
        @delete deleteGameResult(@path id: int32): void;
    }
}