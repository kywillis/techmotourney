declare module 'jquery-bracket' {
    interface BracketOptions {
      init?: object; // Your bracket initialization data
      save?: (data: object) => void; // Callback function to save data
      userData?: object; // Custom user data
      disableToolbar?: boolean;
      disableTeamEdit?: boolean;
      teamWidth?: number;
      scoreWidth?: number;
      matchMargin?: number;
      roundMargin?: number;
      initScore?: boolean;
      dir?: string;
    }
  
    interface JQuery {
      bracket(options: BracketOptions): JQuery;
    }
  }
  